namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Olive;

    public class Prompt : Dialog
    {
        readonly Stack Stack = new();
        public readonly TextView Description = new TextView().Id("Description");
        public readonly TextInput Input = new() { Id = "Input", SpellChecking = SpellCheckingType.No, KeyboardActionType = KeyboardActionType.Done };
        public readonly Button OKButton = new() { Id = "OKButton", Text = "OK", CssClass = "primary-button" };
        public readonly Button CancelButton = new() { Id = "CancelButton", Text = "Cancel" };

        public Prompt(string title, string description = null)
        {
            Title.Text = title;
            Description.Text = description;
        }

        public override async Task OnInitializing()
        {
            await base.OnInitializing();

            await Title.IgnoredAsync(Title.Text.IsEmpty());
            await Description.IgnoredAsync(Description.Text.IsEmpty());

            await Content.Add(Stack);

            await Stack.Add(Description);
            await Stack.Add(Input);

            await ButtonsRow.Add(CancelButton.On(x => x.Tapped, () => Nav.HidePopUp()));
            await ButtonsRow.Add(OKButton);
        }
    }
}