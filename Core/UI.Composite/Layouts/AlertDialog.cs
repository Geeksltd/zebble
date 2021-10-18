namespace Zebble
{
    using System.Threading.Tasks;
    using Olive;

    public class AlertDialog : Dialog
    {
        public readonly TextView Message = new TextView().Id("Message");

        public AlertDialog(string title, string message = null)
        {
            Title.Text = title;
            Message.Text = message;
            ScrollContent = false;
        }

        public override async Task OnInitializing()
        {
            await base.OnInitializing();

            if (Title.Text.IsEmpty()) await Remove(Title);

            if (Message.Text.HasValue()) await Content.Add(Message);
        }
    }
}