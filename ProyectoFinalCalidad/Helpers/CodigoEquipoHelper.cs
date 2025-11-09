using System;
using System.Text;

//ESTA SECCION ES ALGO AUXILIAR PARA APOYAR

namespace ProyectoFinalCalidad.Helpers
{
    public static class CodigoEquipoHelper
    {
        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();

        public static string GenerarCodigoUnico(string nombreEquipo, DateTime fechaRegistro)
        {
            // Formato: EQP-NOMBREEQUIPO-YYYYMMDD-XXXXXX
            // Ejemplo: EQP-ROUTER-20251104-ABC123

            // 1. Prefijo fijo
            string prefijo = "EQP";

            // 2. Nombre del equipo en mayúsculas, sin espacios y limitado a 15 caracteres
            string nombreFormateado = nombreEquipo
                .ToUpper()
                .Replace(" ", "")
                .Replace("-", "")
                .Substring(0, Math.Min(nombreEquipo.Length, 15));

            // 3. Fecha en formato YYYYMMDD
            string fechaFormateada = fechaRegistro.ToString("yyyyMMdd");

            // 4. Cadena aleatoria de 6 caracteres (letras y números)
            string aleatorio = GenerarCadenaAleatoria(6);

            // Combinar todo
            return $"{prefijo}-{nombreFormateado}-{fechaFormateada}-{aleatorio}";
        }

        private static string GenerarCadenaAleatoria(int longitud)
        {
            lock (syncLock) // Thread-safe
            {
                const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                StringBuilder resultado = new StringBuilder(longitud);

                for (int i = 0; i < longitud; i++)
                {
                    resultado.Append(caracteres[random.Next(caracteres.Length)]);
                }

                return resultado.ToString();
            }
        }

        // Método alternativo para generar códigos con más entropía
        public static string GenerarCodigoUnicoSeguro(string nombreEquipo, DateTime fechaRegistro, int equipoId)
        {
            // Formato: EQP-NOMBREEQUIPO-YYYYMMDD-ID-XXXX
            // Esto asegura que incluso si se registran dos equipos con el mismo nombre
            // al mismo tiempo, tendrán códigos diferentes por el ID

            string prefijo = "EQP";
            string nombreFormateado = nombreEquipo
                .ToUpper()
                .Replace(" ", "")
                .Replace("-", "")
                .Substring(0, Math.Min(nombreEquipo.Length, 10));

            string fechaFormateada = fechaRegistro.ToString("yyyyMMdd");
            string aleatorio = GenerarCadenaAleatoria(4);

            return $"{prefijo}-{nombreFormateado}-{fechaFormateada}-{equipoId}-{aleatorio}";
        }

        // Genera subcódigo para unidad específica basado en el código principal
        // Formato: EQP-NOMBRE-YYYYMMDD-<seq><letter>
        public static string GenerarCodigoUnidad(string nombreEquipo, DateTime fechaReferencia, int secuencia)
        {
            string prefijo = "EQP";
            string nombreFormateado = nombreEquipo
                .ToUpper()
                .Replace(" ", "")
                .Replace("-", "");
            string fecha = fechaReferencia.ToString("yyyyMMdd");

            // Sufijo letra A-Z basado en secuencia (cíclico)
            char letra = (char)('A' + ((secuencia - 1) % 26));
            string seq = secuencia.ToString("D3");

            return $"{prefijo}-{nombreFormateado}-{fecha}-{seq}{letra}";
        }
    }
}