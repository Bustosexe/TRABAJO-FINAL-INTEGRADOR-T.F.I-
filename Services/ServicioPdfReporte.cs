using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StockSuper.Models.ViewModels;

namespace StockSuper.Services
{
    // Genera el PDF del reporte de stock con la misma paleta del sistema
    // (azul marino y gris acero). Recibe el modelo ya calculado, por eso el PDF
    // y la pantalla siempre muestran los mismos números.
    public class ServicioPdfReporte
    {
        private const string ColorAzul = "#14263F";
        private const string ColorAcero = "#5B6F86";
        private const string ColorFondo = "#EAEEF3";
        private const string ColorBorde = "#D9DFE7";
        private static readonly CultureInfo Cultura = new("es-AR");

        public byte[] GenerarReporteStock(
            ReporteStockViewModel m, string empresa, string alcance, string cuenta)
        {
            var generado = DateTime.UtcNow.AHoraArgentina();

            return Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4);
                    pagina.Margin(30);
                    pagina.DefaultTextStyle(t => t.FontSize(9));

                    // Encabezado: empresa, alcance y período
                    pagina.Header().Column(col =>
                    {
                        col.Item().Text(empresa).FontSize(15).SemiBold().FontColor(ColorAzul);
                        col.Item().Text("Reporte de stock").FontSize(11);
                        col.Item().PaddingTop(3).Text(
                            $"Período: {m.Desde:dd/MM/yyyy} al {m.Hasta:dd/MM/yyyy}   |   {alcance}")
                            .FontColor(ColorAcero);
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(ColorAzul);
                    });

                    pagina.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(10);

                        // Totales del período
                        col.Item().Row(fila =>
                        {
                            fila.Spacing(6);
                            Indicador(fila, "Stock al inicio", m.TotalInicial);
                            Indicador(fila, "Entradas", m.TotalEntradas);
                            Indicador(fila, "Salidas", m.TotalSalidas);
                            Indicador(fila, "Stock al cierre", m.TotalFinal);
                        });

                        if (!m.Filas.Any())
                        {
                            col.Item().Text("No hay movimientos ni existencias para el período y los filtros elegidos.")
                                .FontColor(ColorAcero);
                            return;
                        }

                        col.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(5);
                                c.RelativeColumn(3);
                                c.ConstantColumn(55);
                                c.ConstantColumn(55);
                                c.ConstantColumn(55);
                                c.ConstantColumn(55);
                            });

                            tabla.Header(h =>
                            {
                                h.Cell().Element(Encabezado).Text("Producto");
                                h.Cell().Element(Encabezado).Text("Categoría");
                                h.Cell().Element(Encabezado).AlignRight().Text("Inicial");
                                h.Cell().Element(Encabezado).AlignRight().Text("Entradas");
                                h.Cell().Element(Encabezado).AlignRight().Text("Salidas");
                                h.Cell().Element(Encabezado).AlignRight().Text("Final");
                            });

                            foreach (var f in m.Filas)
                            {
                                tabla.Cell().Element(Celda).Column(c =>
                                {
                                    c.Item().Text(f.Producto);
                                    c.Item().Text(f.Sku).FontSize(7.5f).FontColor(ColorAcero);
                                });
                                tabla.Cell().Element(Celda).Text(f.Categoria);
                                tabla.Cell().Element(Celda).AlignRight().Text(Numero(f.StockInicial));
                                tabla.Cell().Element(Celda).AlignRight().Text(Numero(f.Entradas));
                                tabla.Cell().Element(Celda).AlignRight().Text(Numero(f.Salidas));
                                tabla.Cell().Element(Celda).AlignRight().Text(Numero(f.StockFinal)).SemiBold();
                            }

                            // Fila de totales
                            tabla.Cell().ColumnSpan(2).Element(CeldaTotal).Text("Total").SemiBold();
                            tabla.Cell().Element(CeldaTotal).AlignRight().Text(Numero(m.TotalInicial)).SemiBold();
                            tabla.Cell().Element(CeldaTotal).AlignRight().Text(Numero(m.TotalEntradas)).SemiBold();
                            tabla.Cell().Element(CeldaTotal).AlignRight().Text(Numero(m.TotalSalidas)).SemiBold();
                            tabla.Cell().Element(CeldaTotal).AlignRight().Text(Numero(m.TotalFinal)).SemiBold();
                        });

                        col.Item().Text("Stock final = stock inicial + entradas - salidas.")
                            .FontSize(8).FontColor(ColorAcero);
                    });

                    // Pie: quién y cuándo generó el documento, y numeración de páginas
                    pagina.Footer().Row(fila =>
                    {
                        fila.RelativeItem().Text($"Generado por {cuenta} el {generado:dd/MM/yyyy HH:mm}")
                            .FontSize(8).FontColor(ColorAcero);
                        fila.AutoItem().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(8).FontColor(ColorAcero));
                            t.Span("Página ");
                            t.CurrentPageNumber();
                            t.Span(" de ");
                            t.TotalPages();
                        });
                    });
                });
            }).GeneratePdf();
        }

        private static string Numero(int valor) => valor.ToString("N0", Cultura);

        private static void Indicador(RowDescriptor fila, string titulo, int valor)
        {
            fila.RelativeItem().Border(1).BorderColor(ColorBorde).Padding(6).Column(c =>
            {
                c.Item().Text(Numero(valor)).FontSize(14).SemiBold().FontColor(ColorAzul);
                c.Item().Text(titulo).FontSize(8).FontColor(ColorAcero);
            });
        }

        private static IContainer Encabezado(IContainer c) =>
            c.Background(ColorFondo).BorderBottom(1).BorderColor(ColorBorde)
             .Padding(4).DefaultTextStyle(x => x.SemiBold().FontColor(ColorAzul));

        private static IContainer Celda(IContainer c) =>
            c.BorderBottom(1).BorderColor(ColorBorde).Padding(4);

        private static IContainer CeldaTotal(IContainer c) =>
            c.Background(ColorFondo).Padding(4);
    }
}