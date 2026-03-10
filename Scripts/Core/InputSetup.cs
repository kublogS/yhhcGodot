using Godot;

public static class InputSetup
{
    public static void EnsureActions()
    {
        Add("move_forward", Key.W);
        Add("move_backward", Key.S);
        Add("move_left", Key.A);
        Add("move_right", Key.D);

        Add("ui_accept", Key.Enter);
        Add("ui_cancel", Key.Escape);
        Add("ui_up", Key.Up);
        Add("ui_down", Key.Down);
        Add("ui_left", Key.Left);
        Add("ui_right", Key.Right);

        Add("menu_up", Key.Up);
        Add("menu_down", Key.Down);
        Add("battle_attack", Key.A);
        Add("battle_defend", Key.D);
        Add("battle_items", Key.I);
        Add("battle_flee", Key.Q);
        Add("toggle_map", Key.Tab);
        Add("toggle_manual", Key.M);
        Add("toggle_pause", Key.Escape);
        Add("slot_up", Key.Up);
        Add("slot_down", Key.Down);
        Add("slot_select", Key.Enter);
        Add("slot_delete", Key.Delete);
        Add("slot_copy", Key.C);
        Add("world_interact", Key.X);
    }

    private static void Add(string action, Key key)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action, 0.2f);
        }

        foreach (var evt in InputMap.ActionGetEvents(action))
        {
            if (evt is InputEventKey k && k.Keycode == key)
            {
                return;
            }
        }

        var inputEvent = new InputEventKey { Keycode = key, PhysicalKeycode = key };
        InputMap.ActionAddEvent(action, inputEvent);
    }
}
