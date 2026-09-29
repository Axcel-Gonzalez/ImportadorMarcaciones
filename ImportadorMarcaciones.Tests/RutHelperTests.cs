using ImportadorMarcaciones.Utils;

namespace ImportadorMarcaciones.Tests
{
    public class RutHelperTests
    {
        [Fact]
        public void TryNormalizarYValidar_RutValidoConPuntos_RetornaRutCanonico()
        {
            const string rutEntrada = "12.345.678-5";

            var esValido = RutHelper.TryNormalizarYValidar(
                rutEntrada,
                out var rutCanonico,
                out var error);

            Assert.True(esValido);
            Assert.Equal("12345678-5", rutCanonico);
            Assert.Empty(error);
        }
    }
}