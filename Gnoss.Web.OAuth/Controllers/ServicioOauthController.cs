using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.LogicaOAuth.OAuth;
using Es.Riam.Gnoss.OAuthAD;
using Es.Riam.Gnoss.Recursos;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.Web.UtilOAuth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
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
        private LoggingService mLoggingService;
        private GnossCache mGnossCache;
        private IHostingEnvironment mEnv;
        private EntityContextOauth mEntityContextOauth;
        private EntityContext mEntityContext;
        private ConfigService mConfigService;
        private IServicesUtilVirtuosoAndReplication mServicesUtilVirtuosoAndReplication;

        /// <summary>
        /// Constructor sin parámetros
        /// </summary>
        public ServicioOauth(GnossCache gnossCache, IHostingEnvironment env, EntityContextOauth entityContextOauth, LoggingService loggingService, EntityContext entityContext, ConfigService configService, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication)
        {
            mGnossCache = gnossCache;
            mEnv = env;
            mLoggingService = loggingService;
            mEntityContext = entityContext;
            mEntityContextOauth = entityContextOauth;
            mConfigService = configService;
            mServicesUtilVirtuosoAndReplication = servicesUtilVirtuosoAndReplication;
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
                OAuthCN oauthCN = new OAuthCN("oauth", mEntityContext, mLoggingService, mConfigService, mEntityContextOauth, mServicesUtilVirtuosoAndReplication);
                oauthCN.EliminarAccessTokenUsuario(pUsuarioID);
                oauthCN.Dispose();
            }
            catch (Exception ex)
            {
                GuardarLogError(ex.Message + "\\n" + ex.StackTrace);
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
                    mGnossOAuthAuthorizationManager = new GnossOAuthAuthorizationManager(mGnossCache, mEnv, mEntityContextOauth, mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
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
                GuardarLogError(ex.Message + "\\n" + ex.StackTrace);
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

        /// <summary>
        /// Guarda el log del error.
        /// </summary>
        [NonAction]
        public void GuardarLogError(string pError)
        {
            string directorio = Path.Combine(mEnv.ContentRootPath, "logs");
            Directory.CreateDirectory(directorio);
            string rutaFichero = Path.Combine(directorio, "log_apiRecursos_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");

            //Si el fichero supera el tamaño máximo lo elimino
            if (System.IO.File.Exists(rutaFichero))
            {
                FileInfo fichero = new FileInfo(rutaFichero);
                if (fichero.Length > 1000000)
                {
                    fichero.Delete();
                }
            }

            //Añado el error al fichero
            using (StreamWriter sw = new StreamWriter(rutaFichero, true, System.Text.Encoding.Default))
            {
                sw.WriteLine(Environment.NewLine + "Fecha: " + DateTime.Now + Environment.NewLine + Environment.NewLine);
                // Escribo el error
                sw.Write(pError);
                sw.WriteLine(Environment.NewLine + Environment.NewLine + "___________________________________________________________________________________________" + Environment.NewLine + Environment.NewLine + Environment.NewLine);
            }
        }
    }
}
