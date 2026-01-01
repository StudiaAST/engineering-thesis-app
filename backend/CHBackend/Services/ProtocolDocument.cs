using CHBackend.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CHBackend.Services
{
    public class ProtocolDocument : IDocument
    {
        private readonly Protocol _model;

        public ProtocolDocument(Protocol model)
        {
            _model = model;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
        }

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text($"Protokół nr: {_model.ProtocolNumber}")
                        .FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

                    column.Item().Text(text =>
                    {
                        text.Span("Data: ").SemiBold();
                        text.Span($"{_model.ReceiptDate:dd.MM.yyyy}");
                    });
                });

                row.ConstantItem(100).Height(50).Placeholder(); // Tutaj można wstawić Logo
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(40).Column(column =>
            {
                column.Spacing(10);

                column.Item().Element(c => TableRow(c, "Rodzaj protokołu", _model.Type));
                column.Item().Element(c => TableRow(c, "Obszar", _model.Area));
                column.Item().Element(c => TableRow(c, "Nr dokumentacji", _model.DocumentNumber));
                column.Item().Element(c => TableRow(c, "Status", _model.StatusDescription));

                var statusText = _model.State == "Open" ? "Otwarty" : "Zamknięty";
                column.Item().Element(c => TableRow(c, "Stan (State)", statusText));

                if (_model.FixDate.HasValue)
                {
                    column.Item().Element(c => TableRow(c, "Data usunięcia usterek", _model.FixDate.Value.ToShortDateString()));
                }
            });
        }

        // Pomocnicza metoda do tworzenia ładnych wierszy
        void TableRow(IContainer container, string label, string value)
        {
            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Row(row =>
            {
                row.RelativeItem().Text(label).SemiBold();
                row.RelativeItem().Text(value ?? "-");
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(x =>
            {
                x.Span("Strona ");
                x.CurrentPageNumber();
            });
        }
    }
}