using ImportadorMarcaciones.Utils;
using Xunit;

namespace ImportadorMarcaciones.Tests
{
    public class CsvSemicolonParserTests
    {
        [Fact]
        public void ParsearLinea_CampoEntreComillasConPuntoYComa_ConservaCuatroColumnas()
        {
            // Arrange
            const string linea =
                "12.345.678-5;2026-09-16 08:02:00;E;\"RELOJ;CENTRAL\"";

            // Act
            var campos = CsvSemicolonParser.ParsearLinea(linea);

            // Assert
            Assert.Equal(4, campos.Length);

            Assert.Equal(
                "12.345.678-5",
                campos[0]);

            Assert.Equal(
                "2026-09-16 08:02:00",
                campos[1]);

            Assert.Equal(
                "E",
                campos[2]);

            Assert.Equal(
                "RELOJ;CENTRAL",
                campos[3]);
        }
    }
}