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

    private static void ApplySlotTheme(Button button, bool isUsed, bool isSelected)
    {
        var fill = isUsed ? PythonColorPalette.SaveSlotFill : PythonColorPalette.ButtonBg;
        var border = isSelected ? PythonColorPalette.Title : PythonColorPalette.PanelBorder;
        button.AddThemeStyleboxOverride("normal", BuildSlotStyle(fill, border, 1));
        button.AddThemeStyleboxOverride("hover", BuildSlotStyle(fill, PythonColorPalette.Title, 2));
        button.AddThemeStyleboxOverride("pressed", BuildSlotStyle(PythonColorPalette.Gray, PythonColorPalette.Title, 2));
        button.AddThemeStyleboxOverride("focus", BuildSlotStyle(fill, PythonColorPalette.Title, 2));
    }

    private static StyleBoxFlat BuildSlotStyle(Color fill, Color border, int width)
    {
        return new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            BorderWidthTop = width,
            BorderWidthBottom = width,
            BorderWidthLeft = width,
            BorderWidthRight = width,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
        };
    }
}
