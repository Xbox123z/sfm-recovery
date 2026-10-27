using Godot;
using System;

/// <summary>
/// Original two-route courier game. All scene art is drawn from geometric primitives;
/// C# owns input, movement/collision, NPC awareness, route progression, UI and save IO.
/// The native C++ route compiler validates reachability and generates route_data.json consumed here.
/// </summary>
public partial class CourierGame : Node2D
{
    private const float ViewWidth = 960f;
    private const float ViewHeight = 540f;
    private const string SavePath = "user://harborlight-courier-save.json";

    private static readonly Color Ink = new Color(0.08f, 0.11f, 0.16f);
    private static readonly Color Muted = new Color(0.34f, 0.40f, 0.47f);
    private static readonly Color Paper = new Color(0.98f, 0.985f, 0.99f);
    private static readonly Color Panel = new Color(1f, 1f, 1f, 1f);
    private static readonly Color Line = new Color(0.77f, 0.81f, 0.86f);
    private static readonly Color Road = new Color(0.91f, 0.93f, 0.96f);
    private static readonly Color Blue = new Color(0.10f, 0.39f, 0.72f);
    private static readonly Color Teal = new Color(0.06f, 0.57f, 0.52f);
    private static readonly Color Gold = new Color(0.96f, 0.69f, 0.19f);
    private static readonly Color Alert = new Color(0.83f, 0.25f, 0.29f);
    private static readonly Color Green = new Color(0.20f, 0.58f, 0.38f);

    private static readonly Rect2 EasyCard = new Rect2(54f, 186f, 407f, 165f);
    private static readonly Rect2 HardCard = new Rect2(499f, 186f, 407f, 165f);
    private static readonly Rect2 UpgradeButton = new Rect2(54f, 388f, 392f, 76f);
    private static readonly Rect2 DebriefButton = new Rect2(315f, 414f, 330f, 64f);
    private static readonly Rect2 PauseButton = new Rect2(881f, 21f, 53f, 42f);

    private readonly CourierSession _session = new CourierSession();
    private RouteCatalog _routeCatalog = new RouteCatalog();
    private Rect2[][] _routeBlocks = Array.Empty<Rect2[]>();
    private Vector2[][] _routeParcels = Array.Empty<Vector2[]>();
    private readonly float[] _npcSuspicion = new float[2];
    private readonly bool[] _investigating = new bool[2];
    private readonly Vector2[] _lastKnownPosition = new Vector2[2];
    private Vector2 _playerPosition = Vector2.Zero;
    private Vector2 _touchMove = Vector2.Zero;
    private Vector2 _touchOrigin = Vector2.Zero;
    private int _moveTouch = -1;
    private int _sprintTouch = -1;
    private bool _sprinting;
    private bool _paused;
    private float _clock;
    private float _toastTime;
    private string _toast = string.Empty;

    public override void _Ready()
    {
        try
        {
            LoadRouteCatalog();
        }
        catch (Exception exception)
        {
            GD.PushError($"Could not load generated route data: {exception.Message}");
            GetTree().Quit(2);
            return;
        }

        foreach (string arg in OS.GetCmdlineUserArgs())
        {
            if (arg == "--smoke-test")
            {
                RunSmokeTests();
                return;
            }
        }

        LoadProgress();
        _playerPosition = ToVector(CurrentLevel().Spawn);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        float dt = Mathf.Clamp((float)delta, 0f, 0.05f);
        _clock += dt;
        if (_toastTime > 0f)
            _toastTime = Mathf.Max(0f, _toastTime - dt);

        if (_session.Phase == CourierPhase.Route && !_paused)
            StepRoute(dt);

        QueueRedraw();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            Key key = keyEvent.Keycode != Key.None ? keyEvent.Keycode : keyEvent.PhysicalKeycode;
            if (key == Key.Escape && _session.Phase == CourierPhase.Route)
            {
                _paused = !_paused;
                GetViewport().SetInputAsHandled();
                return;
            }

            if ((key == Key.E || key == Key.Space) && _session.Phase == CourierPhase.Route && !_paused)
            {
                Interact();
                GetViewport().SetInputAsHandled();
                return;
            }

            if ((key == Key.Enter || key == Key.Space) && _session.Phase == CourierPhase.Debrief)
            {
                ReturnToDispatch();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
        {
            HandleTap(mouse.Position);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventScreenTouch touch)
        {
            if (touch.Pressed)
                HandleTouchDown(touch);
            else
                HandleTouchUp(touch);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventScreenDrag drag && drag.Index == _moveTouch)
        {
            Vector2 offset = drag.Position - _touchOrigin;
            _touchMove = offset / 58f;
            if (_touchMove.Length() > 1f)
                _touchMove = _touchMove.Normalized();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Draw()
    {
        if (_session.Phase == CourierPhase.Dispatch)
            DrawDispatch();
        else if (_session.Phase == CourierPhase.Route)
            DrawRoute();
        else
            DrawDebrief();

        if (_paused)
            DrawPauseOverlay();
    }

    private void StepRoute(float delta)
    {
        Vector2 movement = ReadMovement();
        _sprinting = movement.LengthSquared() > 0.001f && (KeyDown(Key.Shift) || _sprintTouch >= 0);
        if (movement.LengthSquared() > 1f)
            movement = movement.Normalized();

        if (movement.LengthSquared() > 0.001f)
        {
            CourierRoute route = CurrentLevel();
            float speed = _sprinting ? route.RunSpeed : route.WalkSpeed;
            MovePlayer(movement * speed * delta);
        }

        UpdateNpcAwareness(delta);
    }

    private Vector2 ReadMovement()
    {
        float x = 0f;
        float y = 0f;
        if (KeyDown(Key.A) || KeyDown(Key.Left)) x -= 1f;
        if (KeyDown(Key.D) || KeyDown(Key.Right)) x += 1f;
        if (KeyDown(Key.W) || KeyDown(Key.Up)) y -= 1f;
        if (KeyDown(Key.S) || KeyDown(Key.Down)) y += 1f;

        Vector2 movement = new Vector2(x, y) + _touchMove;
        return movement.Length() > 1f ? movement.Normalized() : movement;
    }

    private static bool KeyDown(Key key)
    {
        return Input.IsKeyPressed(key) || Input.IsPhysicalKeyPressed(key);
    }

    private void MovePlayer(Vector2 step)
    {
        TryMoveAxis(new Vector2(step.X, 0f));
        TryMoveAxis(new Vector2(0f, step.Y));
        _playerPosition = new Vector2(
            Mathf.Clamp(_playerPosition.X, 28f, ViewWidth - 28f),
            Mathf.Clamp(_playerPosition.Y, 22f, ViewHeight - 22f));
    }

    private void TryMoveAxis(Vector2 step)
    {
        Vector2 candidate = _playerPosition + step;
        if (!IsBlockedByWalls(candidate, CurrentBlocks()))
            _playerPosition = candidate;
    }

    private bool CanWatcherSeePlayer(int index, Vector2 watcherPosition, Vector2 facing)
    {
        Vector2 toPlayer = _playerPosition - watcherPosition;
        float distance = toPlayer.Length();
        float range = CurrentLevel().VisionRange;
        if (distance <= 1f || distance > range)
            return false;

        Vector2 direction = toPlayer / distance;
        if (facing.Dot(direction) < 0.68f)
            return false;
        return !SegmentBlockedByAnyWall(watcherPosition, _playerPosition, CurrentBlocks());
    }

    private void UpdateNpcAwareness(float delta)
    {
        CourierRoute route = CurrentLevel();
        int watcherCount = route.Patrols.Length;
        float highestSuspicion = 0f;
        bool caughtInDirectSight = false;

        for (int index = 0; index < watcherCount; index++)
        {
            GetWatcherPose(index, out Vector2 position, out Vector2 facing);
            bool visible = CanWatcherSeePlayer(index, position, facing);
            float distance = _playerPosition.DistanceTo(position);
            bool heardRun = _sprinting && distance <= route.HearingRange;

            if (heardRun)
                _lastKnownPosition[index] = _playerPosition;

            float visionRate = route.VisionRate;
            float noiseRate = route.NoiseRate;
            if (_session.HasQuietSoles)
                noiseRate *= 0.45f;

            if (visible)
                _npcSuspicion[index] += visionRate * delta;
            if (heardRun)
                _npcSuspicion[index] += noiseRate * delta;

            if (heardRun && _npcSuspicion[index] >= 28f)
                _investigating[index] = true;
            if (!visible && !heardRun)
            {
                _npcSuspicion[index] = Mathf.Max(0f, _npcSuspicion[index] - route.SuspicionDecay * delta);
                if (_npcSuspicion[index] < 24f)
                    _investigating[index] = false;
            }

            if (visible && _npcSuspicion[index] >= 100f)
                caughtInDirectSight = true;
            highestSuspicion = Mathf.Max(highestSuspicion, _npcSuspicion[index]);
        }

        _session.SetNotice(highestSuspicion);
        if (caughtInDirectSight && _session.FailRoute())
        {
            _playerPosition = ToVector(route.Spawn);
            _paused = false;
            SaveProgress();
        }
    }

    private void GetWatcherPose(int index, out Vector2 position, out Vector2 facing)
    {
        RoutePatrol patrol = CurrentLevel().Patrols[index];
        Vector2 axis = ToVector(patrol.Axis);
        float phase = _clock * patrol.Frequency + patrol.Phase;
        position = ToVector(patrol.Origin) + axis * (Mathf.Sin(phase) * patrol.Amplitude);
        facing = Mathf.Cos(phase) >= 0f ? axis : -axis;

        if (index < _investigating.Length && _investigating[index])
        {
            Vector2 toLastKnown = _lastKnownPosition[index] - position;
            if (toLastKnown.LengthSquared() > 1f)
                facing = toLastKnown.Normalized();
        }
    }

    private void Interact()
    {
        if (_session.Phase != CourierPhase.Route)
            return;

        Vector2[] parcels = CurrentParcels();
        int nearest = -1;
        float nearestDistance = float.MaxValue;
        for (int index = 0; index < _session.RequiredParcelCount; index++)
        {
            if (_session.IsParcelDelivered(index))
                continue;
            float distance = _playerPosition.DistanceTo(parcels[index]);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = index;
            }
        }

        if (nearest >= 0 && nearestDistance <= 43f && _session.TryDeliverParcel(nearest))
        {
            ShowToast($"Parcel logged: {_session.DeliveredCount}/{_session.RequiredParcelCount}");
            return;
        }

        if (_playerPosition.DistanceTo(CurrentExitPosition) <= 63f)
        {
            if (_session.DeliveredCount == 0)
            {
                ShowToast("Take at least one parcel before returning.");
                return;
            }
            _session.SettleAtDispatch();
            SaveProgress();
            return;
        }

        ShowToast("Move close to a parcel or the Dispatch gate, then press ACT.");
    }

    private void HandleTap(Vector2 position)
    {
        if (_session.Phase == CourierPhase.Dispatch)
        {
            if (EasyCard.HasPoint(position))
                StartRoute(0);
            else if (HardCard.HasPoint(position))
                StartRoute(1);
            else if (UpgradeButton.HasPoint(position))
                BuyUpgrade();
            return;
        }

        if (_session.Phase == CourierPhase.Debrief)
        {
            if (DebriefButton.HasPoint(position))
                ReturnToDispatch();
            return;
        }

        if (PauseButton.HasPoint(position))
        {
            _paused = !_paused;
            return;
        }
        if (!_paused)
            Interact();
    }

    private void HandleTouchDown(InputEventScreenTouch touch)
    {
        if (_session.Phase == CourierPhase.Route)
        {
            if (touch.Position.X < 270f && touch.Position.Y > 270f)
            {
                _moveTouch = touch.Index;
                _touchOrigin = touch.Position;
                _touchMove = Vector2.Zero;
                return;
            }
            if (touch.Position.X > 730f && touch.Position.Y >= 425f)
            {
                Interact();
                return;
            }
            if (touch.Position.X > 730f && touch.Position.Y >= 327f && touch.Position.Y < 425f)
            {
                _sprintTouch = touch.Index;
                return;
            }
            if (PauseButton.HasPoint(touch.Position))
            {
                _paused = !_paused;
                return;
            }
        }

        HandleTap(touch.Position);
    }

    private void HandleTouchUp(InputEventScreenTouch touch)
    {
        if (touch.Index == _moveTouch)
        {
            _moveTouch = -1;
            _touchMove = Vector2.Zero;
        }
        if (touch.Index == _sprintTouch)
            _sprintTouch = -1;
    }

    private void StartRoute(int levelIndex)
    {
        CourierRoute route = _routeCatalog.Levels[levelIndex];
        if (!_session.BeginRoute(levelIndex))
            return;

        Array.Clear(_npcSuspicion, 0, _npcSuspicion.Length);
        Array.Clear(_investigating, 0, _investigating.Length);
        Array.Clear(_lastKnownPosition, 0, _lastKnownPosition.Length);
        _playerPosition = ToVector(route.Spawn);
        _clock = 0f;
        _paused = false;
        _touchMove = Vector2.Zero;
        _sprintTouch = -1;
        ShowToast($"{route.Name}: {route.RequiredParcels} parcels, {route.Patrols.Length} patrol(s).");
    }

    private void BuyUpgrade()
    {
        if (_session.TryBuyQuietSoles())
        {
            SaveProgress();
            ShowToast("Quiet soles installed. Sprint noise is reduced.");
        }
        else if (_session.HasQuietSoles)
            ShowToast("Quiet soles are already installed.");
        else
            ShowToast($"Quiet soles cost {CourierSession.QuietSolesPrice} coins.");
    }

    private void ReturnToDispatch()
    {
        _session.ReturnToDispatch();
        _playerPosition = ToVector(CurrentLevel().Spawn);
        _paused = false;
        _touchMove = Vector2.Zero;
        _sprintTouch = -1;
        SaveProgress();
    }

    private void ShowToast(string message)
    {
        _toast = message;
        _toastTime = 2.6f;
    }

    private void LoadRouteCatalog()
    {
        Godot.FileAccess? file = Godot.FileAccess.Open("res://route_data.json", Godot.FileAccess.ModeFlags.Read);
        if (file is null)
            throw new InvalidOperationException($"route_data.json open failed: {Godot.FileAccess.GetOpenError()}");

        string json = file.GetAsText();
        file.Close();
        _routeCatalog = RouteCatalog.Parse(json);
        _routeBlocks = new Rect2[_routeCatalog.Levels.Length][];
        _routeParcels = new Vector2[_routeCatalog.Levels.Length][];

        for (int levelIndex = 0; levelIndex < _routeCatalog.Levels.Length; levelIndex++)
        {
            CourierRoute route = _routeCatalog.Levels[levelIndex];
            _routeBlocks[levelIndex] = new Rect2[route.Blocks.Length];
            _routeParcels[levelIndex] = new Vector2[route.Parcels.Length];
            for (int index = 0; index < route.Blocks.Length; index++)
            {
                RouteRect block = route.Blocks[index];
                _routeBlocks[levelIndex][index] = new Rect2(block.X, block.Y, block.Width, block.Height);
            }
            for (int index = 0; index < route.Parcels.Length; index++)
                _routeParcels[levelIndex][index] = ToVector(route.Parcels[index]);
        }
    }

    private void LoadProgress()
    {
        _session.Restore(0, false, false, false);
        if (!Godot.FileAccess.FileExists(SavePath))
            return;

        Godot.FileAccess? file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Read);
        if (file is null)
            return;
        string json = file.GetAsText();
        file.Close();
        _session.RestorePersistentState(json);
    }

    private void SaveProgress()
    {
        Godot.FileAccess? file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Write);
        if (file is null)
        {
            GD.PushWarning($"Could not save local progress: {Godot.FileAccess.GetOpenError()}");
            return;
        }
        file.StoreString(_session.SerializePersistentState());
        file.Close();
    }

    private CourierRoute CurrentLevel() => _routeCatalog.Levels[_session.CurrentLevelIndex];
    private static Vector2 ToVector(RoutePoint point) => new Vector2(point.X, point.Y);
    private Rect2[] CurrentBlocks() => _routeBlocks[_session.CurrentLevelIndex];
    private Vector2[] CurrentParcels() => _routeParcels[_session.CurrentLevelIndex];
    private Vector2 CurrentExitPosition => ToVector(CurrentLevel().Exit);

    private static bool IsBlockedByWalls(Vector2 point, Rect2[] blocks)
    {
        foreach (Rect2 block in blocks)
        {
            Rect2 padded = new Rect2(block.Position - new Vector2(13f, 13f), block.Size + new Vector2(26f, 26f));
            if (padded.HasPoint(point))
                return true;
        }
        return false;
    }

    private static bool SegmentBlockedByAnyWall(Vector2 from, Vector2 to, Rect2[] blocks)
    {
        foreach (Rect2 block in blocks)
        {
            if (SegmentHitsRect(from, to, block))
                return true;
        }
        return false;
    }

    private static bool SegmentHitsRect(Vector2 from, Vector2 to, Rect2 rect)
    {
        const int samples = 32;
        for (int index = 1; index < samples; index++)
        {
            float t = (float)index / samples;
            if (rect.HasPoint(from.Lerp(to, t)))
                return true;
        }
        return false;
    }

    private void DrawDispatch()
    {
        DrawRect(new Rect2(0f, 0f, ViewWidth, ViewHeight), Paper);
        DrawRect(new Rect2(0f, 0f, ViewWidth, 10f), Blue);
        DrawLabel("HARBORLIGHT COURIER", new Vector2(54f, 78f), 34, Ink);
        DrawLabel("Two routes. Keep a low profile. Return to bank the shift.", new Vector2(56f, 111f), 17, Muted);
        DrawSquarePanel(new Rect2(744f, 35f, 162f, 64f), new Color(0.93f, 0.96f, 0.99f));
        DrawLabel("WALLET", new Vector2(761f, 59f), 12, Muted);
        DrawLabel($"{_session.Wallet} COINS", new Vector2(761f, 84f), 20, Ink);

        CourierRoute easy = _routeCatalog.Levels[0];
        CourierRoute hard = _routeCatalog.Levels[1];
        DrawSquarePanel(EasyCard, Panel);
        DrawRect(new Rect2(EasyCard.Position, new Vector2(9f, EasyCard.Size.Y)), Teal);
        DrawLabel($"LEVEL 1 / {easy.Difficulty}", new Vector2(80f, 222f), 13, Teal);
        DrawLabel(easy.Name, new Vector2(80f, 257f), 25, Ink);
        DrawLabel($"{easy.RequiredParcels} parcels  |  {easy.Patrols.Length} patrol  |  wide paths", new Vector2(80f, 289f), 15, Muted);
        DrawLabel(_session.EasyCompleted ? "BEST: CLEARED" : "CLICK / TAP TO TAKE SHIFT", new Vector2(80f, 326f), 13, _session.EasyCompleted ? Green : Blue);

        DrawSquarePanel(HardCard, Panel);
        DrawRect(new Rect2(HardCard.Position, new Vector2(9f, HardCard.Size.Y)), Alert);
        DrawLabel($"LEVEL 2 / {hard.Difficulty}", new Vector2(525f, 222f), 13, Alert);
        DrawLabel(hard.Name, new Vector2(525f, 257f), 25, Ink);
        DrawLabel($"{hard.RequiredParcels} parcels  |  {hard.Patrols.Length} patrols  |  tight lanes", new Vector2(525f, 289f), 15, Muted);
        DrawLabel(_session.HardCompleted ? "BEST: CLEARED" : "CLICK / TAP TO TAKE SHIFT", new Vector2(525f, 326f), 13, _session.HardCompleted ? Green : Blue);

        DrawSquarePanel(UpgradeButton, Panel);
        DrawRect(new Rect2(UpgradeButton.Position, new Vector2(9f, UpgradeButton.Size.Y)), Gold);
        DrawLabel(_session.HasQuietSoles ? "QUIET SOLES / INSTALLED" : $"QUIET SOLES / {_session.Wallet} OF {CourierSession.QuietSolesPrice} COINS", new Vector2(78f, 418f), 16, Ink);
        DrawLabel(_session.HasQuietSoles ? "Reduces sprint noise on both routes." : "Buy once to reduce sprint noise on both routes.", new Vector2(78f, 445f), 13, Muted);

        DrawLabel("MOUSE / TAP: choose a route     WASD / ARROWS: move     SHIFT: run     E / SPACE: interact", new Vector2(54f, 500f), 13, Muted);
        DrawLabel("Noise can turn a patrol toward its source; direct sight builds suspicion further.", new Vector2(54f, 521f), 12, Muted);
        DrawToast();
    }

    private void DrawRoute()
    {
        DrawRect(new Rect2(0f, 0f, ViewWidth, ViewHeight), Paper);
        DrawRect(new Rect2(0f, 0f, ViewWidth, ViewHeight), new Color(0.94f, 0.97f, 0.99f));

        for (int x = 0; x <= 960; x += 48)
            DrawLine(new Vector2(x, 0f), new Vector2(x, ViewHeight), new Color(0.88f, 0.92f, 0.95f), 1f);
        for (int y = 0; y <= 540; y += 48)
            DrawLine(new Vector2(0f, y), new Vector2(ViewWidth, y), new Color(0.88f, 0.92f, 0.95f), 1f);

        DrawRect(new Rect2(0f, 202f, ViewWidth, 154f), Road);
        DrawRect(new Rect2(350f, 0f, 125f, ViewHeight), Road);
        DrawRect(new Rect2(0f, 264f, ViewWidth, 3f), new Color(0.77f, 0.82f, 0.87f));
        DrawRect(new Rect2(409f, 0f, 3f, ViewHeight), new Color(0.77f, 0.82f, 0.87f));
        DrawRouteDecor();

        foreach (Rect2 block in CurrentBlocks())
            DrawBuilding(block);

        DrawExit();
        DrawWatcherCones();
        DrawParcels();
        DrawWatchers();
        DrawPlayer();
        DrawRouteHud();
        if (_toastTime > 0f)
            DrawToast();
    }

    private void DrawRouteDecor()
    {
        for (int x = 18; x < 940; x += 70)
        {
            DrawRect(new Rect2(x, 220f, 26f, 4f), new Color(0.79f, 0.83f, 0.87f));
            DrawRect(new Rect2(x, 331f, 26f, 4f), new Color(0.79f, 0.83f, 0.87f));
        }
        DrawRect(new Rect2(20f, 176f, 48f, 15f), new Color(0.78f, 0.88f, 0.81f));
        DrawRect(new Rect2(206f, 174f, 48f, 15f), new Color(0.78f, 0.88f, 0.81f));
        DrawRect(new Rect2(543f, 365f, 48f, 13f), new Color(0.78f, 0.88f, 0.81f));
        DrawRect(new Rect2(882f, 171f, 48f, 15f), new Color(0.78f, 0.88f, 0.81f));
    }

    private void DrawBuilding(Rect2 rect)
    {
        DrawRect(new Rect2(rect.Position + new Vector2(4f, 5f), rect.Size), new Color(0.77f, 0.81f, 0.85f));
        DrawRect(rect, Panel);
        DrawRect(new Rect2(rect.Position, new Vector2(rect.Size.X, 10f)), Ink);
        DrawRect(rect, Ink, false, 2f);

        float windowY = rect.Position.Y + 27f;
        for (float windowX = rect.Position.X + 17f; windowX < rect.End.X - 14f; windowX += 38f)
        {
            DrawRect(new Rect2(windowX, windowY, 17f, 15f), new Color(0.78f, 0.87f, 0.93f));
            DrawRect(new Rect2(windowX, windowY + 27f, 17f, 15f), new Color(0.88f, 0.92f, 0.95f));
        }
        DrawRect(new Rect2(rect.Position.X + 9f, rect.End.Y - 14f, rect.Size.X - 18f, 5f), new Color(0.89f, 0.92f, 0.95f));
    }

    private void DrawExit()
    {
        Vector2 exit = CurrentExitPosition;
        Rect2 exitRect = new Rect2(exit - new Vector2(22.5f, 24.5f), new Vector2(45f, 49f));
        DrawRect(exitRect, new Color(0.85f, 0.96f, 0.89f));
        DrawRect(exitRect, Green, false, 3f);
        DrawLabel("HQ", exit + new Vector2(-13f, 6f), 17, Green);
    }

    private void DrawWatcherCones()
    {
        CourierRoute route = CurrentLevel();
        int count = route.Patrols.Length;
        float range = route.VisionRange;
        for (int index = 0; index < count; index++)
        {
            GetWatcherPose(index, out Vector2 position, out Vector2 facing);
            Vector2 left = position + facing.Rotated(-0.56f) * range;
            Vector2 right = position + facing.Rotated(0.56f) * range;
            DrawColoredPolygon(new[] { position, left, right }, new Color(0.87f, 0.25f, 0.29f, 0.10f));
        }
    }

    private void DrawParcels()
    {
        Vector2[] parcels = CurrentParcels();
        for (int index = 0; index < _session.RequiredParcelCount; index++)
        {
            Vector2 point = parcels[index];
            if (_session.IsParcelDelivered(index))
            {
                DrawRect(new Rect2(point - new Vector2(12f, 12f), new Vector2(24f, 24f)), new Color(0.83f, 0.91f, 0.87f));
                DrawRect(new Rect2(point - new Vector2(12f, 12f), new Vector2(24f, 24f)), Green, false, 2f);
                DrawLine(point + new Vector2(-6f, 0f), point + new Vector2(-1f, 5f), Green, 3f);
                DrawLine(point + new Vector2(-1f, 5f), point + new Vector2(8f, -6f), Green, 3f);
            }
            else
            {
                DrawRect(new Rect2(point - new Vector2(13f, 13f), new Vector2(26f, 26f)), Gold);
                DrawRect(new Rect2(point - new Vector2(13f, 13f), new Vector2(26f, 26f)), Ink, false, 2f);
                DrawLine(point + new Vector2(-7f, -3f), point + new Vector2(7f, -3f), Ink, 2f);
                DrawLine(point + new Vector2(0f, -8f), point + new Vector2(0f, 8f), Ink, 1f);
            }
        }
    }

    private void DrawWatchers()
    {
        int count = CurrentLevel().Patrols.Length;
        for (int index = 0; index < count; index++)
        {
            GetWatcherPose(index, out Vector2 position, out Vector2 facing);
            DrawCircle(position + new Vector2(0f, 8f), 13f, new Color(0.20f, 0.24f, 0.29f, 0.16f));
            DrawRect(new Rect2(position.X - 10f, position.Y - 8f, 20f, 22f), new Color(0.16f, 0.31f, 0.48f));
            DrawRect(new Rect2(position.X - 7f, position.Y - 19f, 14f, 13f), new Color(0.92f, 0.73f, 0.58f));
            DrawRect(new Rect2(position.X - 10f, position.Y - 21f, 20f, 5f), Ink);
            DrawLine(position, position + facing * 17f, Gold, 3f);
            if (_investigating[index])
                DrawRect(new Rect2(position.X - 4f, position.Y - 33f, 8f, 8f), Gold);
        }
    }

    private void DrawPlayer()
    {
        DrawCircle(_playerPosition + new Vector2(0f, 10f), 14f, new Color(0.12f, 0.18f, 0.24f, 0.18f));
        DrawRect(new Rect2(_playerPosition.X - 11f, _playerPosition.Y - 7f, 22f, 25f), Blue);
        DrawRect(new Rect2(_playerPosition.X - 8f, _playerPosition.Y - 20f, 16f, 15f), new Color(0.95f, 0.76f, 0.62f));
        DrawRect(new Rect2(_playerPosition.X - 11f, _playerPosition.Y - 22f, 22f, 5f), Ink);
        DrawRect(new Rect2(_playerPosition.X + 5f, _playerPosition.Y + 1f, 10f, 10f), Gold);
    }

    private void DrawRouteHud()
    {
        CourierRoute route = CurrentLevel();
        string level = $"{route.Name.ToUpperInvariant()} / {route.Difficulty}";
        DrawSquarePanel(new Rect2(18f, 16f, 360f, 56f), Panel);
        DrawLabel(level, new Vector2(31f, 39f), 14, Ink);
        DrawLabel($"PARCELS  {_session.DeliveredCount}/{_session.RequiredParcelCount}", new Vector2(31f, 60f), 13, Muted);

        DrawSquarePanel(new Rect2(600f, 16f, 270f, 56f), Panel);
        DrawLabel("NOTICE", new Vector2(615f, 38f), 13, Muted);
        DrawRect(new Rect2(615f, 47f, 232f, 13f), new Color(0.90f, 0.92f, 0.94f));
        float width = 232f * (_session.Notice / 100f);
        if (width > 0f)
            DrawRect(new Rect2(615f, 47f, width, 13f), _session.Notice >= 72f ? Alert : Gold);
        DrawRect(new Rect2(615f, 47f, 232f, 13f), Ink, false, 1f);
        DrawLabel(AnyInvestigator() ? "PATROL INVESTIGATING" : "SUSPICION FADES OUT OF SIGHT", new Vector2(615f, 84f), 11, AnyInvestigator() ? Alert : Muted);

        DrawRect(PauseButton, Panel);
        DrawRect(PauseButton, Ink, false, 2f);
        DrawLabel("II", new Vector2(899f, 49f), 18, Ink);
        DrawLabel("WASD / ARROWS: MOVE    SHIFT: RUN    E / SPACE: ACT    ESC: PAUSE", new Vector2(22f, 514f), 13, Ink);
        DrawLabel("Noise turns patrols toward its source; a sustained clear sightline can end the route.", new Vector2(22f, 533f), 11, Muted);

        if (OS.HasFeature("android"))
            DrawTouchControls();
    }

    private bool AnyInvestigator()
    {
        for (int index = 0; index < CurrentLevel().Patrols.Length; index++)
            if (_investigating[index])
                return true;
        return false;
    }

    private void DrawTouchControls()
    {
        Vector2 center = new Vector2(122f, 428f);
        DrawCircle(center, 64f, new Color(0.95f, 0.97f, 0.99f, 0.89f));
        DrawCircle(center, 64f, new Color(0.25f, 0.33f, 0.42f, 0.70f));
        DrawCircle(center + _touchMove * 36f, 23f, new Color(0.12f, 0.39f, 0.72f, 0.72f));
        DrawSquareButton(new Rect2(786f, 337f, 134f, 61f), "RUN", _sprintTouch >= 0 ? Gold : Panel);
        DrawSquareButton(new Rect2(786f, 411f, 134f, 61f), "ACT", Panel);
        DrawLabel("MOVE", new Vector2(99f, 509f), 12, Muted);
    }

    private void DrawDebrief()
    {
        DrawRect(new Rect2(0f, 0f, ViewWidth, ViewHeight), Paper);
        DrawRect(new Rect2(0f, 0f, ViewWidth, 10f), _session.LastRouteFailed ? Alert : Green);
        DrawSquarePanel(new Rect2(140f, 82f, 680f, 360f), Panel);
        DrawLabel(_session.LastRouteFailed ? "ROUTE INTERRUPTED" : "SHIFT DEBRIEF", new Vector2(190f, 142f), 30, _session.LastRouteFailed ? Alert : Green);
        DrawLabel(_session.CurrentLevelIndex == 0 ? "PIERLINE GREEN / LEVEL 1" : "GLASSHOUSE ARCADE / LEVEL 2", new Vector2(192f, 177f), 15, Muted);
        DrawRect(new Rect2(190f, 199f, 580f, 2f), Line);
        DrawWrappedMessage(_session.ResultMessage, new Vector2(192f, 241f), 16, Ink, 70);
        DrawLabel($"WALLET: {_session.Wallet} COINS", new Vector2(192f, 320f), 19, Ink);
        DrawLabel($"PARCELS LOGGED: {_session.DeliveredCount}/{_session.RequiredParcelCount}", new Vector2(192f, 349f), 15, Muted);
        DrawSquareButton(DebriefButton, "RETURN TO DISPATCH", new Color(0.92f, 0.96f, 0.99f));
        DrawLabel("Press ENTER or tap the button.", new Vector2(386f, 498f), 13, Muted);
    }

    private void DrawPauseOverlay()
    {
        DrawRect(new Rect2(0f, 0f, ViewWidth, ViewHeight), new Color(0.05f, 0.08f, 0.12f, 0.72f));
        DrawSquarePanel(new Rect2(300f, 188f, 360f, 164f), Panel);
        DrawLabel("PAUSED", new Vector2(424f, 242f), 30, Ink);
        DrawLabel("Press ESC or tap the pause button to resume.", new Vector2(344f, 285f), 14, Muted);
        DrawLabel("The route clock and awareness are stopped.", new Vector2(349f, 314f), 13, Muted);
    }

    private void DrawToast()
    {
        if (_toastTime <= 0f || string.IsNullOrEmpty(_toast))
            return;
        DrawSquarePanel(new Rect2(236f, 450f, 488f, 42f), new Color(1f, 1f, 1f, 0.96f));
        DrawLabel(_toast, new Vector2(253f, 477f), 14, Ink);
    }

    private void DrawSquarePanel(Rect2 rect, Color fill)
    {
        DrawRect(rect, fill);
        DrawRect(rect, Ink, false, 2f);
    }

    private void DrawSquareButton(Rect2 rect, string label, Color fill)
    {
        DrawRect(rect, fill);
        DrawRect(rect, Ink, false, 2f);
        int size = label.Length > 16 ? 14 : 17;
        DrawLabel(label, new Vector2(rect.Position.X + 14f, rect.Position.Y + rect.Size.Y * 0.60f), size, Ink);
    }

    private void DrawWrappedMessage(string message, Vector2 start, int size, Color color, int maxChars)
    {
        string[] words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string line = string.Empty;
        float y = start.Y;
        foreach (string word in words)
        {
            string next = line.Length == 0 ? word : line + " " + word;
            if (next.Length > maxChars)
            {
                DrawLabel(line, new Vector2(start.X, y), size, color);
                y += size + 12f;
                line = word;
            }
            else
                line = next;
        }
        if (line.Length > 0)
            DrawLabel(line, new Vector2(start.X, y), size, color);
    }

    private void DrawLabel(string text, Vector2 position, int size, Color color)
    {
        Font font = ThemeDB.FallbackFont;
        if (font is null)
            return;
        DrawString(font, position, text, HorizontalAlignment.Left, -1f, size, color);
    }

    private void RunSmokeTests()
    {
        int passed = 0;
        try
        {
            void Check(string name, bool condition)
            {
                if (!condition)
                    throw new InvalidOperationException("FAIL: " + name);
                passed++;
                GD.Print("PASS: " + name);
            }

            Check("exactly two route definitions", CourierSession.LevelCount == 2);
            Check("easy route is 3 parcels / 1 patrol", CourierSession.RequiredParcelsFor(0) == 3 && CourierSession.PatrolCountFor(0) == 1);
            Check("hard route is 4 parcels / 2 patrols", CourierSession.RequiredParcelsFor(1) == 4 && CourierSession.PatrolCountFor(1) == 2);
            Check("C++-generated route catalog is loaded", _routeCatalog.Version == 1 && _routeCatalog.Levels.Length == 2);
            Check("C# runtime uses generated map geometry", _routeBlocks.Length == 2 && _routeBlocks[0].Length == 6 && _routeParcels[1].Length == 4);
            Check("generated patrol parameters match both route designs", _routeCatalog.Levels[0].Patrols.Length == 1 && _routeCatalog.Levels[1].Patrols.Length == 2);

            var session = new CourierSession();
            session.Restore(0, false, false, false);
            Check("begin easy route", session.BeginRoute(0));
            Check("reject invalid parcel index", !session.TryDeliverParcel(3));
            Check("first parcel delivered once", session.TryDeliverParcel(0) && !session.TryDeliverParcel(0));
            Check("remaining easy parcels delivered", session.TryDeliverParcel(1) && session.TryDeliverParcel(2));
            Check("full easy route settles and pays 36", session.SettleAtDispatch() && session.Wallet == 36 && session.EasyCompleted);
            session.ReturnToDispatch();
            Check("quiet soles purchase deducts 35", session.TryBuyQuietSoles() && session.Wallet == 1 && session.HasQuietSoles);

            Check("begin hard route", session.BeginRoute(1));
            Check("hard route records temporary pay", session.TryDeliverParcel(0) && session.ShiftPay == CourierSession.HardParcelPay);
            session.AddNotice(100f);
            Check("full suspicion alone does not trigger failure", session.Phase == CourierPhase.Route && !session.LastRouteFailed);
            Check("separate caught event ends the route", session.FailRoute() && session.LastRouteFailed && session.Phase == CourierPhase.Debrief);
            Check("failure loses only unbanked pay", session.Wallet == 1 && session.ShiftPay == 0 && !session.HardCompleted);

            session.ReturnToDispatch();
            Check("retry hard route after failure", session.BeginRoute(1));
            Check("all hard-route parcels deliver", session.TryDeliverParcel(0) && session.TryDeliverParcel(1) && session.TryDeliverParcel(2) && session.TryDeliverParcel(3));
            Check("full hard route settles and pays 68", session.SettleAtDispatch() && session.Wallet == 69 && session.HardCompleted);
            string saved = session.SerializePersistentState();
            var restored = new CourierSession();
            Check("save reload preserves wallet, upgrade and clear flags", restored.RestorePersistentState(saved) && restored.Wallet == 69 && restored.HasQuietSoles && restored.EasyCompleted && restored.HardCompleted);

            Rect2 wall = new Rect2(40f, 10f, 20f, 40f);
            Check("vision ray intersects a building", SegmentHitsRect(new Vector2(0f, 30f), new Vector2(100f, 30f), wall));
            Check("vision ray outside building stays clear", !SegmentHitsRect(new Vector2(0f, 0f), new Vector2(100f, 0f), wall));
            Check("player collision blocks building interior", IsBlockedByWalls(new Vector2(50f, 30f), new[] { wall }));
            Check("player collision permits open lane", !IsBlockedByWalls(new Vector2(10f, 30f), new[] { wall }));

            GD.Print($"SMOKE_TESTS_PASSED={passed}");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"SMOKE_TESTS_FAILED after {passed}: {exception}");
            GetTree().Quit(1);
        }
    }
}
