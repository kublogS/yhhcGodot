using System;
using System.Collections.Generic;
using Godot;

public partial class RewardController : Control
{
    private GameSession _session = null!;
    private SaveService _saveService = null!;
    private RichTextLabel _log = null!;
    private RewardFlowCoordinator _flow = null!;
    private HBoxContainer _replaceRow = null!;
    private readonly List<Button> _replaceButtons = new();
    private AudioStreamPlayer _jinglePlayer = null!;
    private bool _embeddedMode;

    public event Action<PersistResult>? PersistResolved;

    public override void _Ready()
    {
        _session = GetNode<GameSession>("/root/GameSession");
        _saveService = GetNode<SaveService>("/root/SaveService");
        _flow = new RewardFlowCoordinator(_session, _saveService);
        _log = GetNode<RichTextLabel>("Root/Log");
        GetNode<Button>("Root/Buttons/Move").Pressed += () => AppendAction(_flow.ClaimMove());
        GetNode<Button>("Root/Buttons/Money").Pressed += () => AppendAction(_flow.ClaimMoney());
        GetNode<Button>("Root/Buttons/Items").Pressed += () => AppendAction(_flow.ClaimItems());
        GetNode<Button>("Root/Buttons/Continue").Pressed += OnContinue;
        _jinglePlayer = new AudioStreamPlayer { Name = "RewardJingle" };
        AddChild(_jinglePlayer);
        BuildReplaceRow();
        if (Visible)
        {
            OpenStandalone();
            return;
        }

        CloseOverlay();
    }

    public bool OpenOverlay()
    {
        _embeddedMode = true;
        return Activate();
    }

    public void CloseOverlay()
    {
        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
    }

    private void OpenStandalone()
    {
        _embeddedMode = false;
        if (!Activate())
        {
            SceneRouteNavigator.Navigate(SceneRoute.MainMenu, GetTree());
        }
    }

    private bool Activate()
    {
        if (!_flow.HasActiveState())
        {
            return false;
        }

        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _jinglePlayer.Stream = BattleSfxLibrary.GetVictoryJingle();
        _jinglePlayer.Play();
        RefreshSummary();
        if (_replaceButtons.Count > 0)
        {
            _replaceButtons[0].GrabFocus();
        }

        return true;
    }

    private void RefreshSummary()
    {
        var summary = _flow.BuildSummary();
        _log.Text =
            $"Battaglia conclusa\n" +
            $"EXP gia assegnata: {summary.Exp}\n" +
            $"Soli disponibili: {summary.Soli}\n" +
            $"Oggetti disponibili: {summary.Items}\n" +
            $"Mosse rubabili: {summary.Tokens}\n";
        RefreshReplaceRow();
    }

    private void AppendAction(RewardActionResult action)
    {
        _log.Text += action.Message + "\n";
        RefreshReplaceRow();
    }

    private void OnContinue()
    {
        var result = _flow.FinishAndPersist();
        if (!result.Success)
        {
            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                _log.Text += result.Message + "\n";
            }
            RefreshReplaceRow();
            if (_embeddedMode)
            {
                PersistResolved?.Invoke(result);
            }
            return;
        }

        if (_embeddedMode)
        {
            PersistResolved?.Invoke(result);
            return;
        }

        SceneRouteNavigator.Navigate(result.Route, GetTree());
    }

    private void BuildReplaceRow()
    {
        _replaceRow = new HBoxContainer { Name = "ReplaceRow", Visible = false };
        GetNode<VBoxContainer>("Root").AddChild(_replaceRow);
        for (var i = 0; i < 5; i++)
        {
            var slotIndex = i;
            var button = new Button();
            button.Pressed += () => AppendAction(_flow.ReplacePendingMoveSlot(slotIndex));
            _replaceButtons.Add(button);
            _replaceRow.AddChild(button);
        }
    }

    private void RefreshReplaceRow()
    {
        if (!_flow.HasPendingMoveReplace())
        {
            _replaceRow.Visible = false;
            return;
        }

        var slots = _flow.BuildMoveReplaceSlots();
        for (var i = 0; i < _replaceButtons.Count; i++)
        {
            _replaceButtons[i].Text = i < slots.Count ? slots[i] : $"Slot {i + 1}";
        }

        _replaceRow.Visible = true;
        _log.Text += $"Scegli lo slot da sostituire per {_flow.PendingMoveName()}.\n";
    }
}
