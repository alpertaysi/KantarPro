namespace KantarPro.Desktop
{
    public static class AutoRefreshPolicy
    {
        public static bool ShouldRefresh(bool isEditingInput, bool isModalDialogOpen, bool hasActiveSelection = false)
        {
            return !isEditingInput && !isModalDialogOpen && !hasActiveSelection;
        }
    }
}



