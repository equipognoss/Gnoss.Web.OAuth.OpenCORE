using Gnoss.Web.OAuth.Application.UseCases.VerifyOAuthRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gnoss.Web.OAuth.Presentation.Controllers
{
    /// <summary>
    /// Operaciones de verificación OAuth 1.0.
    /// </summary>
    [ApiController]
    [Route("ServicioOauth")]
    [EnableRateLimiting("oauth")]
    public sealed class OAuthController(IVerifyOAuthRequestUseCase useCase) : ControllerBase
    {
        /// <summary>
        /// Verifica la firma OAuth 1.0 de una URL firmada y devuelve el GUID del usuario propietario de los tokens.
        /// </summary>
        /// <remarks>
        /// El parámetro <c>pUrl</c> debe ser una URL absoluta que contenga los siguientes parámetros OAuth en la query string:
        /// <c>oauth_consumer_key</c>, <c>oauth_token</c>, <c>oauth_signature</c>, <c>oauth_timestamp</c>,
        /// <c>oauth_nonce</c>, <c>oauth_signature_method</c> y <c>oauth_version</c>.
        ///
        /// El servicio realiza las siguientes comprobaciones en orden:
        /// 1. Parámetros OAuth obligatorios presentes.
        /// 2. Timestamp dentro de la ventana de ±5 minutos.
        /// 3. Nonce no utilizado previamente (anti-replay via Redis).
        /// 4. Token activo en base de datos (State = 1).
        /// 5. Consumer registrado en base de datos.
        /// 6. Firma HMAC-SHA1 válida.
        /// </remarks>
        /// <param name="pUrl">URL completa con los parámetros OAuth firmados en la query string (máx. 4096 caracteres).</param>
        /// <param name="pMetodoHttp">Método HTTP con el que se firmó la petición (GET, POST, PUT, DELETE...).</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>GUID del usuario propietario de los tokens si la firma es válida.</returns>
        /// <response code="200">Firma válida. Devuelve el GUID del usuario.</response>
        /// <response code="400">Parámetros de entrada inválidos o URL demasiado larga.</response>
        /// <response code="401">Firma inválida, token inexistente o revocado, nonce repetido o timestamp expirado.</response>
        /// <response code="429">Límite de peticiones superado. Consultar cabecera <c>Retry-After</c>.</response>
        [HttpGet("ObtenerUsuarioAPartirDeUrl")]
        [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ObtenerUsuarioAPartirDeUrl(
            [FromQuery] string pUrl,
            [FromQuery] string pMetodoHttp,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(pUrl))
                return BadRequest("El parámetro pUrl es obligatorio.");

            if (string.IsNullOrWhiteSpace(pMetodoHttp))
                return BadRequest("El parámetro pMetodoHttp es obligatorio.");

            var result = await useCase.ExecuteAsync(pUrl, pMetodoHttp, ct);

            return result.Status switch
            {
                VerifyOAuthStatus.Success => Ok(result.UsuarioID!.Value),
                VerifyOAuthStatus.UrlTooLong => BadRequest("El parámetro pUrl excede la longitud máxima permitida."),
                _ => Unauthorized()
            };
        }
    }
}
