using Godot;

public partial class SavesMenuController
{
    private void AskDeleteSlot(int index)
    {
        if (!IsUsedSlot(index))
        {
            SetStatus($"Slot {index + 1} è già vuoto.");
            return;
        }

        _pendingDeleteIndex = index;
        _deleteConfirm.DialogText = $"Eliminare definitivamente il salvataggio nello Slot {index + 1}?";
        _deleteConfirm.PopupCentered();
    }

    private void ConfirmDelete()
    {
        if (_pendingDeleteIndex < 0)
        {
            return;
        }

        var deleted = _flow.DeleteSlotByIndex(_pendingDeleteIndex);
        if (_copySourceIndex == _pendingDeleteIndex)
        {
            _copySourceIndex = -1;
        }

        SetStatus(deleted
            ? $"Slot {_pendingDeleteIndex + 1} eliminato."
            : $"Nessun salvataggio eliminato in slot {_pendingDeleteIndex + 1}.");
        _pendingDeleteIndex = -1;
        Refresh();
        FocusCurrent();
    }

    private void PressCopy(int index)
    {
        if (_copySourceIndex < 0)
        {
            if (!IsUsedSlot(index))
            {
                SetStatus($"Slot {index + 1} vuoto: scegli una sorgente occupata.");
                return;
            }

            _copySourceIndex = index;
            SetStatus($"Sorgente copia: Slot {index + 1}. Seleziona destinazione e premi Duplica/Incolla.");
            Refresh();
            return;
        }

        if (_copySourceIndex == index)
        {
            _copySourceIndex = -1;
            SetStatus("Copia annullata.");
            Refresh();
            return;
        }

        TryCopy(_copySourceIndex, index, false);
    }

    private void TryCopy(int sourceIndex, int targetIndex, bool overwrite)
    {
        var result = _flow.CopySlotByIndex(sourceIndex, targetIndex, overwrite);
        if (result == SaveCopyResult.TargetOccupied && !overwrite)
        {
            _pendingOverwriteSourceIndex = sourceIndex;
            _pendingOverwriteTargetIndex = targetIndex;
            _overwriteConfirm.DialogText = $"Slot {targetIndex + 1} è occupato. Sovrascrivere con Slot {sourceIndex + 1}?";
            _overwriteConfirm.PopupCentered();
            return;
        }

        _copySourceIndex = -1;
        SetStatus(result == SaveCopyResult.Success
            ? $"Salvataggio copiato: Slot {sourceIndex + 1} -> Slot {targetIndex + 1}."
            : $"Copia fallita: {result}.");
        Refresh();
        FocusCurrent();
    }

    private void ConfirmOverwriteCopy()
    {
        if (_pendingOverwriteSourceIndex < 0 || _pendingOverwriteTargetIndex < 0)
        {
            return;
        }

        TryCopy(_pendingOverwriteSourceIndex, _pendingOverwriteTargetIndex, true);
        _pendingOverwriteSourceIndex = -1;
        _pendingOverwriteTargetIndex = -1;
    }

    private static void ApplySlotTheme(
        PanelContainer card,
        Button selectButton,
        Button deleteButton,
        Button copyButton,
        bool isUsed,
        bool isSelected,
        bool isCopyTarget)
    {
        var fill = isUsed ? new Color(0.13f, 0.21f, 0.17f, 0.9f) : new Color(0.11f, 0.16f, 0.14f, 0.84f);
        var border = isSelected
            ? new Color(0.45f, 0.75f, 0.57f, 1f)
            : (isCopyTarget ? new Color(0.35f, 0.61f, 0.47f, 1f) : new Color(0.21f, 0.35f, 0.28f, 0.95f));
        card.AddThemeStyleboxOverride("panel", BuildSlotStyle(fill, border, isSelected ? 3 : 2, 14));

        var selectFill = isSelected ? new Color(0.29f, 0.53f, 0.4f, 0.95f) : new Color(0.21f, 0.37f, 0.29f, 0.92f);
        var selectBorder = isSelected ? new Color(0.55f, 0.82f, 0.65f, 1f) : new Color(0.39f, 0.61f, 0.49f, 1f);
        selectButton.Modulate = new Color(0.9f, 0.98f, 0.93f, 1f);
        selectButton.AddThemeStyleboxOverride("normal", BuildSlotStyle(selectFill, selectBorder, 2, 10));
        selectButton.AddThemeStyleboxOverride("hover", BuildSlotStyle(selectFill.Lightened(0.08f), selectBorder, 2, 10));
        selectButton.AddThemeStyleboxOverride("pressed", BuildSlotStyle(selectFill.Darkened(0.08f), selectBorder.Darkened(0.1f), 2, 10));

        deleteButton.Modulate = new Color(0.84f, 0.92f, 0.88f, deleteButton.Disabled ? 0.5f : 1f);
        copyButton.Modulate = new Color(0.84f, 0.92f, 0.88f, copyButton.Disabled ? 0.5f : 1f);
        deleteButton.AddThemeStyleboxOverride("normal", BuildSlotStyle(new Color(0.16f, 0.27f, 0.22f, 0.94f), new Color(0.3f, 0.48f, 0.39f, 1f), 1, 8));
        copyButton.AddThemeStyleboxOverride("normal", BuildSlotStyle(new Color(0.16f, 0.27f, 0.22f, 0.94f), new Color(0.3f, 0.48f, 0.39f, 1f), 1, 8));
    }

    private static StyleBoxFlat BuildSlotStyle(Color fill, Color border, int width, int radius)
    {
        return new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            BorderWidthTop = width,
            BorderWidthBottom = width,
            BorderWidthLeft = width,
            BorderWidthRight = width,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
        };
    }
}
