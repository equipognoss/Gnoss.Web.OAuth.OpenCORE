using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.Trazas;
using Es.Riam.Gnoss.LogicaOAuth.OAuth;
using Es.Riam.Gnoss.OAuthAD;
using Es.Riam.Gnoss.Recursos;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.Web.UtilOAuth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;

namespace Gnoss.Web.OAuth
{
    /// <summary>
    /// Servicio para comunicarse con otras aplicaciones mediante validación oauth
    /// </summary>
    [System.ComponentModel.ToolboxItem(false)]
    // Para permitir que se llame a este servicio web desde un script, usando ASP.NET AJAX, quite la marca de comentario de la línea siguiente. 
    // [System.Web.Script.Services.ScriptService]
    [ApiController]
    [Route("[controller]")]
    public class ServicioOauth : Controller
    {
        private static DateTime HORA_COMPROBACION_TRAZA;

        private LoggingService mLoggingService;
        private GnossCache mGnossCache;
        private IHostingEnvironment mEnv;
        private EntityContextOauth mEntityContextOauth;
        private EntityContext mEntityContext;
        private ConfigService mConfigService;
        private RedisCacheWrapper mRedisCacheWrapper;
        private IServicesUtilVirtuosoAndReplication mServicesUtilVirtuosoAndReplication;
        private ILogger mLogger;
        private ILoggerFactory mLoggerFactory;

        /// <summary>
        /// Constructor sin parámetros
        /// </summary>
        public ServicioOauth(GnossCache gnossCache, IHostingEnvironment env, EntityContextOauth entityContextOauth, LoggingService loggingService, EntityContext entityContext, ConfigService configService, RedisCacheWrapper redisCacheWrapper, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication, ILogger<ServicioOauth> logger, ILoggerFactory loggerFactory)
        {
            mGnossCache = gnossCache;
            mEnv = env;
            mLoggingService = loggingService;
            mEntityContext = entityContext;
            mEntityContextOauth = entityContextOauth;
            mConfigService = configService;
            mServicesUtilVirtuosoAndReplication = servicesUtilVirtuosoAndReplication;
            mRedisCacheWrapper = redisCacheWrapper;
            mLogger = logger;
            mLoggerFactory = loggerFactory;
        }

        /// <summary>
        /// Elimina el token de un usuario 
        /// </summary>
        /// <param name="pUsuarioID"></param>
        [HttpPost]
        [Route("EliminarTokensUsuario")]
        public void EliminarTokensUsuario(Guid pUsuarioID)
        {
            try
            {
                OAuthCN oauthCN = new OAuthCN("oauth", mEntityContext, mLoggingService, mConfigService, mEntityContextOauth, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<OAuthCN>(), mLoggerFactory);
                oauthCN.EliminarAccessTokenUsuario(pUsuarioID);
                oauthCN.Dispose();
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, mLogger);
                throw ex;
            }
            finally
            {
                ControladorConexiones.CerrarConexiones();
            }
        }

        private GnossOAuthAuthorizationManager mGnossOAuthAuthorizationManager;

        private GnossOAuthAuthorizationManager GnossOAuthAuthorizationManager
        {
            get
            {
                if (mGnossOAuthAuthorizationManager == null)
                {
                    mGnossOAuthAuthorizationManager = new GnossOAuthAuthorizationManager(mGnossCache, mEnv, mEntityContextOauth, mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<GnossOAuthAuthorizationManager>(), mLoggerFactory);
                }
                return mGnossOAuthAuthorizationManager;
            }
        }

        /// <summary>
        /// Obtiene el ID del usuario al que pertence el token que lleva la URL.
        /// </summary>
        /// <param name="pUrl">URL OAuth</param>
        /// <param name="pMetodoHttp">Método HTTP con el que se firmó la petición</param>
        /// <returns>ID del usuario al que pertence el token que lleva la URL</returns>
        [HttpGet]
        [Route("ObtenerUsuarioAPartirDeUrl")]
        public Guid ObtenerUsuarioAPartirDeUrl(string pUrl, string pMetodoHttp)
        {
            try
            {
                return GnossOAuthAuthorizationManager.ObtenerUsuarioIDDePeticionGet(WebUtility.UrlDecode(pUrl), pMetodoHttp);
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, mLogger);
                throw;
            }
            finally
            {
                mLoggingService.GuardarTraza(ObtenerRutaTraza());
                ControladorConexiones.CerrarConexiones();
            }
        }

        private string ObtenerRutaTraza()
        {
            string ruta = Path.Combine(mEnv.ContentRootPath, "trazas");

            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }
            ruta = Path.Combine(ruta, "traza_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");

            return ruta;
        }

        #region Métodos de trazas
        [NonAction]
        private void IniciarTraza()
        {
            if (DateTime.Now > HORA_COMPROBACION_TRAZA)
            {
                HORA_COMPROBACION_TRAZA = DateTime.Now.AddSeconds(15);
                TrazasCL trazasCL = new TrazasCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<TrazasCL>(), mLoggerFactory);
                string tiempoTrazaResultados = trazasCL.ObtenerTrazaEnCache("oauth");

                if (!string.IsNullOrEmpty(tiempoTrazaResultados))
                {
                    int valor = 0;
                    int.TryParse(tiempoTrazaResultados, out valor);
                    LoggingService.TrazaHabilitada = true;
                    LoggingService.TiempoMinPeticion = valor; //Para sacar los segundos
                }
                else
                {
                    LoggingService.TrazaHabilitada = false;
                    LoggingService.TiempoMinPeticion = 0;
                }
            }
        }

        #endregion

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            IniciarTraza();
        }
    }
}
