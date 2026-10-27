#include <algorithm>
#include <array>
#include <cmath>
#include <cstdint>
#include <fstream>
#include <iomanip>
#include <iostream>
#include <queue>
#include <string>
#include <vector>

namespace {
struct Point { float x; float y; };
struct Rect { float x; float y; float width; float height; };
struct Patrol {
    Point origin;
    Point axis;
    float amplitude;
    float phase;
    float frequency;
};
struct Level {
    int id;
    const char* name;
    const char* difficulty;
    int requiredParcels;
    int parcelPay;
    int fullBonus;
    float walkSpeed;
    float runSpeed;
    float visionRange;
    float hearingRange;
    float visionRate;
    float noiseRate;
    float suspicionDecay;
    Point spawn;
    Point exit;
    std::vector<Rect> blocks;
    std::vector<Point> parcels;
    std::vector<Patrol> patrols;
};

const std::vector<Level> kLevels = {
    {
        0, "Pierline Green", "EASY", 3, 8, 12, 150.0f, 236.0f,
        230.0f, 118.0f, 19.0f, 10.0f, 13.0f, {145.0f, 270.0f}, {58.0f, 270.0f},
        {
            {150.0f, 55.0f, 175.0f, 105.0f}, {430.0f, 48.0f, 180.0f, 112.0f},
            {718.0f, 60.0f, 172.0f, 95.0f}, {165.0f, 382.0f, 175.0f, 91.0f},
            {468.0f, 376.0f, 182.0f, 98.0f}, {742.0f, 380.0f, 148.0f, 93.0f}
        },
        {{270.0f, 258.0f}, {570.0f, 304.0f}, {820.0f, 260.0f}},
        {{{456.0f, 177.0f}, {1.0f, 0.0f}, 54.0f, 0.0f, 0.48f}}
    },
    {
        1, "Glasshouse Arcade", "HARD", 4, 12, 20, 141.0f, 222.0f,
        258.0f, 166.0f, 28.0f, 17.0f, 13.0f, {145.0f, 270.0f}, {58.0f, 270.0f},
        {
            {112.0f, 45.0f, 166.0f, 101.0f}, {344.0f, 48.0f, 174.0f, 108.0f},
            {646.0f, 43.0f, 190.0f, 104.0f}, {112.0f, 386.0f, 171.0f, 91.0f},
            {363.0f, 386.0f, 176.0f, 94.0f}, {646.0f, 378.0f, 190.0f, 101.0f},
            {838.0f, 186.0f, 82.0f, 57.0f}
        },
        {{252.0f, 253.0f}, {464.0f, 300.0f}, {700.0f, 255.0f}, {820.0f, 314.0f}},
        {
            {{390.0f, 174.0f}, {1.0f, 0.0f}, 55.0f, 0.0f, 0.62f},
            {{710.0f, 320.0f}, {0.0f, 1.0f}, 42.0f, 0.8f, 0.43f}
        }
    }
};

constexpr float kPlayerRadius = 13.0f;
constexpr float kInteractRadius = 43.0f;
constexpr float kMinX = 28.0f;
constexpr float kMaxX = 932.0f;
constexpr float kMinY = 22.0f;
constexpr float kMaxY = 518.0f;
constexpr float kGridStep = 8.0f;

bool containsExpanded(const Rect& r, Point p, float padding) {
    return p.x >= r.x - padding && p.x <= r.x + r.width + padding &&
           p.y >= r.y - padding && p.y <= r.y + r.height + padding;
}

bool blocked(const Level& level, Point p) {
    for (const Rect& r : level.blocks)
        if (containsExpanded(r, p, kPlayerRadius)) return true;
    return false;
}

float distance(Point a, Point b) {
    const float dx = a.x - b.x;
    const float dy = a.y - b.y;
    return std::sqrt(dx * dx + dy * dy);
}

bool segmentHitsRect(Point from, Point to, const Rect& rect) {
    constexpr int samples = 32;
    for (int i = 1; i < samples; ++i) {
        const float t = static_cast<float>(i) / static_cast<float>(samples);
        Point p{from.x + (to.x - from.x) * t, from.y + (to.y - from.y) * t};
        if (p.x >= rect.x && p.x <= rect.x + rect.width &&
            p.y >= rect.y && p.y <= rect.y + rect.height)
            return true;
    }
    return false;
}

bool canReach(const Level& level, Point target) {
    const int columns = static_cast<int>((kMaxX - kMinX) / kGridStep) + 1;
    const int rows = static_cast<int>((kMaxY - kMinY) / kGridStep) + 1;
    const int startX = std::clamp(static_cast<int>(std::round((level.spawn.x - kMinX) / kGridStep)), 0, columns - 1);
    const int startY = std::clamp(static_cast<int>(std::round((level.spawn.y - kMinY) / kGridStep)), 0, rows - 1);
    const int start = startY * columns + startX;
    std::vector<uint8_t> visited(static_cast<size_t>(columns * rows), 0);
    std::queue<int> pending;
    visited[static_cast<size_t>(start)] = 1;
    pending.push(start);
    const std::array<std::array<int, 2>, 4> directions{{{{1, 0}}, {{-1, 0}}, {{0, 1}}, {{0, -1}}}};

    while (!pending.empty()) {
        const int current = pending.front();
        pending.pop();
        const int x = current % columns;
        const int y = current / columns;
        const Point point{kMinX + static_cast<float>(x) * kGridStep,
                          kMinY + static_cast<float>(y) * kGridStep};
        if (distance(point, target) <= kInteractRadius) return true;
        for (const auto& direction : directions) {
            const int nx = x + direction[0];
            const int ny = y + direction[1];
            if (nx < 0 || nx >= columns || ny < 0 || ny >= rows) continue;
            const size_t next = static_cast<size_t>(ny * columns + nx);
            if (visited[next]) continue;
            const Point neighbor{kMinX + static_cast<float>(nx) * kGridStep,
                                 kMinY + static_cast<float>(ny) * kGridStep};
            if (blocked(level, neighbor)) continue;
            visited[next] = 1;
            pending.push(static_cast<int>(next));
        }
    }
    return false;
}

bool validate(std::ostream& out) {
    bool okay = true;
    auto check = [&out, &okay](bool condition, const std::string& label) {
        out << (condition ? "PASS: " : "FAIL: ") << label << '\n';
        okay = okay && condition;
    };
    check(kLevels.size() == 2, "exactly two authored routes");
    check(segmentHitsRect({0.0f, 30.0f}, {100.0f, 30.0f}, {40.0f, 10.0f, 20.0f, 40.0f}),
          "ray/rectangle occlusion detects a wall");
    check(!segmentHitsRect({0.0f, 0.0f}, {100.0f, 0.0f}, {40.0f, 10.0f, 20.0f, 40.0f}),
          "ray/rectangle test leaves clear path unobstructed");

    for (const Level& level : kLevels) {
        const std::string prefix = std::string(level.name) + ": ";
        check(level.requiredParcels == static_cast<int>(level.parcels.size()),
              prefix + "parcel count matches route objective");
        check(!level.patrols.empty() && level.patrols.size() == (level.id == 0 ? 1u : 2u),
              prefix + "patrol count matches difficulty design");
        check(!blocked(level, level.spawn) && !blocked(level, level.exit),
              prefix + "spawn and Dispatch gate are outside collision geometry");
        check(canReach(level, level.exit), prefix + "Dispatch gate is reachable from spawn");
        for (size_t i = 0; i < level.parcels.size(); ++i)
            check(!blocked(level, level.parcels[i]) && canReach(level, level.parcels[i]),
                  prefix + "parcel " + std::to_string(i + 1) + " is reachable");
        for (size_t i = 0; i < level.patrols.size(); ++i) {
            const Point p = level.patrols[i].origin;
            bool inWall = false;
            for (const Rect& r : level.blocks)
                if (containsExpanded(r, p, 0.0f)) inWall = true;
            check(!inWall, prefix + "patrol " + std::to_string(i + 1) + " starts outside buildings");
        }
    }
    return okay;
}

void writePoint(std::ostream& out, Point p) {
    out << "{\"x\":" << p.x << ",\"y\":" << p.y << '}';
}

void writeJson(std::ostream& out) {
    out << "{\"version\":1,\"levels\":[";
    for (size_t levelIndex = 0; levelIndex < kLevels.size(); ++levelIndex) {
        if (levelIndex) out << ',';
        const Level& level = kLevels[levelIndex];
        out << "{\"id\":" << level.id
            << ",\"name\":\"" << level.name << "\""
            << ",\"difficulty\":\"" << level.difficulty << "\""
            << ",\"requiredParcels\":" << level.requiredParcels
            << ",\"parcelPay\":" << level.parcelPay
            << ",\"fullBonus\":" << level.fullBonus
            << ",\"walkSpeed\":" << level.walkSpeed
            << ",\"runSpeed\":" << level.runSpeed
            << ",\"visionRange\":" << level.visionRange
            << ",\"hearingRange\":" << level.hearingRange
            << ",\"visionRate\":" << level.visionRate
            << ",\"noiseRate\":" << level.noiseRate
            << ",\"suspicionDecay\":" << level.suspicionDecay
            << ",\"spawn\":";
        writePoint(out, level.spawn);
        out << ",\"exit\":";
        writePoint(out, level.exit);
        out << ",\"blocks\":[";
        for (size_t i = 0; i < level.blocks.size(); ++i) {
            if (i) out << ',';
            const Rect& r = level.blocks[i];
            out << "{\"x\":" << r.x << ",\"y\":" << r.y
                << ",\"width\":" << r.width << ",\"height\":" << r.height << '}';
        }
        out << "],\"parcels\":[";
        for (size_t i = 0; i < level.parcels.size(); ++i) {
            if (i) out << ',';
            writePoint(out, level.parcels[i]);
        }
        out << "],\"patrols\":[";
        for (size_t i = 0; i < level.patrols.size(); ++i) {
            if (i) out << ',';
            const Patrol& p = level.patrols[i];
            out << "{\"origin\":";
            writePoint(out, p.origin);
            out << ",\"axis\":";
            writePoint(out, p.axis);
            out << ",\"amplitude\":" << p.amplitude
                << ",\"phase\":" << p.phase
                << ",\"frequency\":" << p.frequency << '}';
        }
        out << "]}";
    }
    out << "]}\n";
}
} // namespace

int main(int argc, char** argv) {
    if (argc != 3 || std::string(argv[1]) != "--write") {
        std::cerr << "usage: route_compiler --write <route_data.json>\n";
        return 2;
    }

    if (!validate(std::cout)) {
        std::cerr << "ROUTE_DATA_VALIDATION=FAIL\n";
        return 1;
    }

    std::ofstream output(argv[2], std::ios::binary | std::ios::trunc);
    if (!output) {
        std::cerr << "could not open output: " << argv[2] << '\n';
        return 3;
    }
    output << std::fixed << std::setprecision(2);
    writeJson(output);
    output.close();
    if (!output) {
        std::cerr << "failed while writing output: " << argv[2] << '\n';
        return 4;
    }
    std::cout << "ROUTE_DATA_VALIDATION=PASS\nWROTE: " << argv[2] << '\n';
    return 0;
}
