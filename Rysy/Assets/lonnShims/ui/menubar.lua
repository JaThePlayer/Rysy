-- Dummied-out version of the file, just so some mods editing the menubar in their main utils file can load.
-- eg. auspicioushelper

local noop = function() end

local menubar = {}

menubar.menubar = {
    {"file", {
        {"new", noop},
        {"open", noop},
        {"recent", noop},
        {},
        {"save", noop},
        {"save_as", noop},
        {},
        {"exit", noop}
    }},
    {"edit", {
        {"undo", noop},
        {"redo", noop},
        {},
        {"settings", noop},
    }},
    {"view", {
        {"view_layer", {
            {"view_tiles_fg", noop, "checkbox", noop},
            {"view_tiles_bg", noop, "checkbox", noop},
            {"view_entities", noop, "checkbox", noop},
            {"view_triggers", noop, "checkbox", noop},
            {"view_decals_fg", noop, "checkbox", noop},
            {"view_decals_bg", noop, "checkbox", noop},
            {"view_trigger_categories", {
                {"view_trigger_category_general", noop, "checkbox", noop},
                {"view_trigger_category_camera", noop, "checkbox", noop},
                {"view_trigger_category_audio", noop, "checkbox", noop},
                {"view_trigger_category_visual", noop, "checkbox", noop},
            }},
        }},
        {"view_only_depended_on", {
            {"view_depended_on_entities", noop, "checkbox", noop},
            {"view_depended_on_triggers", noop, "checkbox", noop},
            {"view_depended_on_decals", noop, "checkbox", noop},
        }},
        {"view_zoom_to_extents", noop},
    }},
    {"map", {
        {"stylegrounds", noop},
        {"metadata", noop},
        {"dependencies", noop},
        {"save_map_image", noop},
    }},
    {"room", {
        {"add", noop},
        {"configure", noop},
        {},
        {"delete", noop}
    }},
    {"help", {
        {"check_for_updates", noop},
        {"open_storage_directory", noop},
        {"about", noop}
    }}
}

return menubar