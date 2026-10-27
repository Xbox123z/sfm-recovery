using System;
using System.Text.Json;

public enum CourierPhase
{
    Dispatch,
    Route,
    Debrief
}

/// <summary>
/// Engine-independent rules/state model for Harborlight Courier.
/// Awareness and a caught/failure event are deliberately separate.
/// </summary>
public sealed class CourierSession
{
    public const int LevelCount = 2;
    public const int QuietSolesPrice = 35;
    public const int EasyParcelPay = 8;
    public const int HardParcelPay = 12;
    public const int EasyFullRouteBonus = 12;
    public const int HardFullRouteBonus = 20;

    private readonly bool[] _delivered = new bool[4];

    public CourierPhase Phase { get; private set; } = CourierPhase.Dispatch;
    public int Wallet { get; private set; }
    public bool HasQuietSoles { get; private set; }
    public bool EasyCompleted { get; private set; }
    public bool HardCompleted { get; private set; }
    public int CurrentLevelIndex { get; private set; }
    public int DeliveredCount { get; private set; }
    public int ShiftPay { get; private set; }
    public float Notice { get; private set; }
    public bool LastRouteFailed { get; private set; }
    public bool LastRouteCompleted { get; private set; }
    public string ResultMessage { get; private set; } = string.Empty;

    public int RequiredParcelCount => RequiredParcelsFor(CurrentLevelIndex);
    public bool IsRouteComplete => DeliveredCount == RequiredParcelCount;

    public static int RequiredParcelsFor(int levelIndex) => levelIndex switch
    {
        0 => 3,
        1 => 4,
        _ => 0
    };

    public static int PatrolCountFor(int levelIndex) => levelIndex switch
    {
        0 => 1,
        1 => 2,
        _ => 0
    };

    public void Restore(int wallet, bool hasQuietSoles, bool easyCompleted, bool hardCompleted)
    {
        Wallet = Math.Max(0, wallet);
        HasQuietSoles = hasQuietSoles;
        EasyCompleted = easyCompleted;
        HardCompleted = hardCompleted;
        CurrentLevelIndex = 0;
        Phase = CourierPhase.Dispatch;
        LastRouteFailed = false;
        LastRouteCompleted = false;
        ResultMessage = string.Empty;
        ResetShift();
    }

    public bool TryBuyQuietSoles()
    {
        if (Phase != CourierPhase.Dispatch || HasQuietSoles || Wallet < QuietSolesPrice)
            return false;

        Wallet -= QuietSolesPrice;
        HasQuietSoles = true;
        return true;
    }

    public bool BeginRoute(int levelIndex)
    {
        if (Phase != CourierPhase.Dispatch || levelIndex < 0 || levelIndex >= LevelCount)
            return false;

        CurrentLevelIndex = levelIndex;
        LastRouteFailed = false;
        LastRouteCompleted = false;
        ResultMessage = string.Empty;
        ResetShift();
        Phase = CourierPhase.Route;
        return true;
    }

    public bool IsParcelDelivered(int index)
    {
        return index >= 0 && index < RequiredParcelCount && _delivered[index];
    }

    public bool TryDeliverParcel(int index)
    {
        if (Phase != CourierPhase.Route || index < 0 || index >= RequiredParcelCount || _delivered[index])
            return false;

        _delivered[index] = true;
        DeliveredCount++;
        ShiftPay += CurrentLevelIndex == 0 ? EasyParcelPay : HardParcelPay;
        return true;
    }

    /// <summary>Updates suspicion only. It never fails a route by itself.</summary>
    public void AddNotice(float amount)
    {
        if (Phase == CourierPhase.Route)
            Notice = Math.Clamp(Notice + amount, 0f, 100f);
    }

    public void SetNotice(float value)
    {
        if (Phase == CourierPhase.Route)
            Notice = Math.Clamp(value, 0f, 100f);
    }

    /// <summary>Called only after the simplified game reports a direct caught event.</summary>
    public bool FailRoute()
    {
        if (Phase != CourierPhase.Route)
            return false;

        Phase = CourierPhase.Debrief;
        LastRouteFailed = true;
        LastRouteCompleted = false;
        ShiftPay = 0;
        ResultMessage = "A patrol caught sight of you. You returned to Dispatch; unbanked shift pay was lost.";
        return true;
    }

    public bool SettleAtDispatch()
    {
        if (Phase != CourierPhase.Route)
            return false;

        int bonus = IsRouteComplete
            ? (CurrentLevelIndex == 0 ? EasyFullRouteBonus : HardFullRouteBonus)
            : 0;
        int payout = ShiftPay + bonus;
        Wallet += payout;
        LastRouteCompleted = IsRouteComplete;

        if (IsRouteComplete)
        {
            if (CurrentLevelIndex == 0)
                EasyCompleted = true;
            else
                HardCompleted = true;

            ResultMessage = $"Full route complete. {payout} coins banked at Dispatch.";
        }
        else if (DeliveredCount > 0)
        {
            ResultMessage = $"Partial return: {DeliveredCount} parcel(s) logged. {payout} coins banked.";
        }
        else
        {
            ResultMessage = "You returned safely without a parcel payout.";
        }

        Phase = CourierPhase.Debrief;
        return true;
    }

    public void ReturnToDispatch()
    {
        if (Phase != CourierPhase.Debrief)
            return;

        Phase = CourierPhase.Dispatch;
        LastRouteFailed = false;
        LastRouteCompleted = false;
        ResultMessage = string.Empty;
        ResetShift();
    }

    public string SerializePersistentState()
    {
        var data = new CourierSaveData
        {
            Wallet = Wallet,
            HasQuietSoles = HasQuietSoles,
            EasyCompleted = EasyCompleted,
            HardCompleted = HardCompleted
        };
        return JsonSerializer.Serialize(data);
    }

    public bool RestorePersistentState(string json)
    {
        try
        {
            CourierSaveData? data = JsonSerializer.Deserialize<CourierSaveData>(json);
            if (data is null)
                throw new JsonException("Empty save data.");

            Restore(data.Wallet, data.HasQuietSoles, data.EasyCompleted, data.HardCompleted);
            return true;
        }
        catch (Exception)
        {
            Restore(0, false, false, false);
            return false;
        }
    }

    private void ResetShift()
    {
        Array.Clear(_delivered, 0, _delivered.Length);
        DeliveredCount = 0;
        ShiftPay = 0;
        Notice = 0f;
    }
}

public sealed class CourierSaveData
{
    public int Wallet { get; set; }
    public bool HasQuietSoles { get; set; }
    public bool EasyCompleted { get; set; }
    public bool HardCompleted { get; set; }
}
