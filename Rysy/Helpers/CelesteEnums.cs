namespace Rysy.Helpers;
public static class CelesteEnums {
    public static Color[] RoomColors = new Color[] {
        Color.White,
        "f6735e".FromRGB(),
        "85f65e".FromRGB(),
        "37d7e3".FromRGB(),
        "376be3".FromRGB(),
        "c337e3".FromRGB(),
        "e33773".FromRGB()
    };

    public static string[] RoomColorNames = new string[] {
        "White",
        "Orange",
        "Green",
        "Light Blue",
        "Blue",
        "Purple",
        "Red"
    };

    public static Dictionary<string, string> Music = new(StringComparer.Ordinal) {
        ["music_levelselect"] = "event:/music/menu/level_select",
        ["music_credits"] = "event:/music/menu/credits",
        ["music_complete_area"] = "event:/music/menu/complete_area",
        ["music_complete_summit"] = "event:/music/menu/complete_summit",
        ["music_complete_bside"] = "event:/music/menu/complete_bside",
        ["music_prologue_intro_vignette"] = "event:/game/00_prologue/intro_vignette",
        ["music_prologue_beginning"] = "event:/music/lvl0/intro",
        ["music_prologue_collapse"] = "event:/music/lvl0/bridge",
        ["music_prologue_title_ping"] = "event:/music/lvl0/title_ping",
        ["music_city"] = "event:/music/lvl1/main",
        ["music_city_theo"] = "event:/music/lvl1/theo",
        ["music_oldsite_beginning"] = "event:/music/lvl2/beginning",
        ["music_oldsite_mirror"] = "event:/music/lvl2/mirror",
        ["music_oldsite_dreamblock_sting_pt1"] = "event:/music/lvl2/dreamblock_sting_pt1",
        ["music_oldsite_dreamblock_sting_pt2"] = "event:/music/lvl2/dreamblock_sting_pt2",
        ["music_oldsite_evil_maddy"] = "event:/music/lvl2/evil_madeline",
        ["music_oldsite_chase"] = "event:/music/lvl2/chase",
        ["music_oldsite_payphone_loop"] = "event:/music/lvl2/phone_loop",
        ["music_oldsite_payphone_end"] = "event:/music/lvl2/phone_end",
        ["music_oldsite_awake"] = "event:/music/lvl2/awake",
        ["music_resort_intro"] = "event:/music/lvl3/intro",
        ["music_resort_explore"] = "event:/music/lvl3/explore",
        ["music_resort_clean"] = "event:/music/lvl3/clean",
        ["music_resort_clean_extended"] = "event:/music/lvl3/clean_extended",
        ["music_resort_oshiro_theme"] = "event:/music/lvl3/oshiro_theme",
        ["music_resort_oshiro_chase"] = "event:/music/lvl3/oshiro_chase",
        ["music_cliffside_main"] = "event:/music/lvl4/main",
        ["music_cliffside_heavywinds"] = "event:/music/lvl4/heavy_winds",
        ["music_cliffside_panicattack"] = "event:/music/lvl4/minigame",
        ["music_temple_normal"] = "event:/music/lvl5/normal",
        ["music_temple_middle"] = "event:/music/lvl5/middle_temple",
        ["music_temple_mirror"] = "event:/music/lvl5/mirror",
        ["music_temple_mirrorcutscene"] = "event:/music/lvl5/mirror_cutscene",
        ["music_reflection_maddietheo"] = "event:/music/lvl6/madeline_and_theo",
        ["music_reflection_starjump"] = "event:/music/lvl6/starjump",
        ["music_reflection_fall"] = "event:/music/lvl6/the_fall",
        ["music_reflection_fight"] = "event:/music/lvl6/badeline_fight",
        ["music_reflection_fight_glitch"] = "event:/music/lvl6/badeline_glitch",
        ["music_reflection_fight_finish"] = "event:/music/lvl6/badeline_acoustic",
        ["music_reflection_main"] = "event:/music/lvl6/main",
        ["music_reflection_secretroom"] = "event:/music/lvl6/secret_room",
        ["music_summit_main"] = "event:/music/lvl7/main",
        ["music_summit_finalascent"] = "event:/music/lvl7/final_ascent",
        ["music_epilogue_main"] = "event:/music/lvl8/main",
        ["music_core_main"] = "event:/music/lvl9/main",
        ["music_pico8_title"] = "event:/classic/pico8_mus_00",
        ["music_pico8_area1"] = "event:/classic/pico8_mus_01",
        ["music_pico8_area2"] = "event:/classic/pico8_mus_02",
        ["music_pico8_area3"] = "event:/classic/pico8_mus_03",
        ["music_pico8_wind"] = "event:/classic/sfx61",
        ["music_pico8_end"] = "event:/classic/sfx62",
        ["music_pico8_boot"] = "event:/classic/pico8_boot",
        ["music_rmx_01_forsaken_city"] = "event:/music/remix/01_forsaken_city",
        ["music_rmx_02_old_site"] = "event:/music/remix/02_old_site",
        ["music_rmx_03_resort"] = "event:/music/remix/03_resort",
        ["music_rmx_04_cliffside"] = "event:/music/remix/04_cliffside",
        ["music_rmx_05_mirror_temple"] = "event:/music/remix/05_mirror_temple",
        ["music_rmx_06_reflection"] = "event:/music/remix/06_reflection",
        ["music_rmx_07_summit"] = "event:/music/remix/07_summit",
        ["music_rmx_09_core"] = "event:/music/remix/09_core",
        ["cas_01_forsaken_city"] = "event:/music/cassette/01_forsaken_city",
        ["cas_02_old_site"] = "event:/music/cassette/02_old_site",
        ["cas_03_resort"] = "event:/music/cassette/03_resort",
        ["cas_04_cliffside"] = "event:/music/cassette/04_cliffside",
        ["cas_05_mirror_temple"] = "event:/music/cassette/05_mirror_temple",
        ["cas_06_reflection"] = "event:/music/cassette/06_reflection",
        ["cas_07_summit"] = "event:/music/cassette/07_summit",
        ["cas_08_core"] = "event:/music/cassette/09_core",
        ["music_farewell_part01"] = "event:/new_content/music/lvl10/part01",
        ["music_farewell_part02"] = "event:/new_content/music/lvl10/part02",
        ["music_farewell_part03"] = "event:/new_content/music/lvl10/part03",
        ["music_farewell_intermission_heartgroove"] = "event:/new_content/music/lvl10/intermission_heartgroove",
        ["music_farewell_intermission_powerpoint"] = "event:/new_content/music/lvl10/intermission_powerpoint",
        ["music_farewell_reconciliation"] = "event:/new_content/music/lvl10/reconciliation",
        ["music_farewell_cassette"] = "event:/new_content/music/lvl10/cassette_rooms",
        ["music_farewell_final_run"] = "event:/new_content/music/lvl10/final_run",
        ["music_farewell_end_cinematic"] = "event:/new_content/music/lvl10/cinematic/end",
        ["music_farewell_end_cinematic_intro"] = "event:/new_content/music/lvl10/cinematic/end_intro",
        ["music_farewell_firstbirdcrash_cinematic"] = "event:/new_content/music/lvl10/cinematic/bird_crash_first",
        ["music_farewell_secondbirdcrash_cinematic"] = "event:/new_content/music/lvl10/cinematic/bird_crash_second",
        ["music_farewell_granny"] = "event:/new_content/music/lvl10/granny_farewell",
        ["music_farewell_golden_room"] = "event:/new_content/music/lvl10/golden_room",
    };
}

public enum WindPatterns {
    None,
    Left,
    Right,
    LeftStrong,
    RightStrong,
    LeftOnOff,
    RightOnOff,
    LeftOnOffFast,
    RightOnOffFast,
    Alternating,
    LeftGemsOnly,
    RightCrazy,
    Down,
    Up,
    Space
}

public enum Inventories {
    Prologue,
    Default,
    OldSite,
    CH6End,
    TheSummit,
    Core,
    Farewell,
}

public enum IntroTypes {
    Transition,
    Respawn,
    WalkInRight,
    WalkInLeft,
    Jump,
    WakeUp,
    Fall,
    TempleMirrorVoid,
    None,
    ThinkForABit
}

public static class Depths {
    public const int BGTerrain = 10000;

    public const int BGMirrors = 9500;

    public const int BGDecals = 9000;

    public const int BGParticles = 8000;

    public const int SolidsBelow = 5000;

    public const int Below = 2000;

    public const int NPCs = 1000;

    public const int TheoCrystal = 100;

    public const int Player = 0;

    public const int Dust = -50;

    public const int Pickups = -100;

    public const int Seeker = -200;

    public const int Particles = -8000;

    public const int Above = -8500;

    public const int Solids = -9000;

    public const int FGTerrain = -10000;

    public const int FGDecals = -10500;

    public const int DreamBlocks = -11000;

    public const int CrystalSpinners = -11500;

    public const int PlayerDreamDashing = -12000;

    public const int Enemy = -12500;

    public const int FakeWalls = -13000;

    public const int FGParticles = -50000;

    public const int Top = -1000000;

    public const int FormationSequences = -2000000;
}