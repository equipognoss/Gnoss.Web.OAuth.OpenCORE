using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.OAuth.Messages;
using Es.Riam.Gnoss.AD.EncapsuladoDatos;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.Logica.Usuarios;
using Es.Riam.Gnoss.LogicaOAuth.OAuth;
using Es.Riam.Gnoss.OAuthAD;
using Es.Riam.Gnoss.OAuthAD.OAuth;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.Util.Seguridad;
using Es.Riam.Gnoss.Web.UtilOAuth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Linq;
using System.Security.Principal;
using System.Threading;

namespace Gnoss.Web.OAuth.Controladores
{
    /// <summary>
    /// Controlador para realizar ciertas acción comunes de la Web.
    /// </summary>
    public class ControladorUtilWeb
    {
        #region Constantes

        /// <summary>
        /// Clave CLAVE_TITULO_LOGIN.
        /// </summary>
        public const string CLAVE_TITULO_LOGIN = "TITULO_LOGIN";
        /// <summary>
        /// Clave CLAVE_TITULO_ACCESO.
        /// </summary>
        public const string CLAVE_TITULO_ACCESO = "TITULO_ACCESO";
        /// <summary>
        /// Clave CLAVE_DESCRIPCION.
        /// </summary>
        public const string CLAVE_DESCRIPCION = "DESCRIPCION";
        /// <summary>
        /// Clave CLAVE_DENEGADO.
        /// </summary>
        public const string CLAVE_DENEGADO = "DENEGADO";
        /// <summary>
        /// Clave CLAVE_NOMBRE.
        /// </summary>
        public const string CLAVE_NOMBRE = "NOMBRE";
        /// <summary>
        /// Clave CLAVE_PASSWORD.
        /// </summary>
        public const string CLAVE_PASSWORD = "PASSWORD";
        /// <summary>
        /// Clave CLAVE_ENTRAR.
        /// </summary>
        public const string CLAVE_ENTRAR = "ENTRAR";
        /// <summary>
        /// Clave CLAVE_ERROR_LOGIN.
        /// </summary>
        public const string CLAVE_ERROR_LOGIN = "ERROR_LOGIN";
        /// <summary>
        /// Clave CLAVE_PERIMITIR.
        /// </summary>
        public const string CLAVE_PERIMITIR = "PERMITIR";
        /// <summary>
        /// Clave CLAVE_DENEGAR.
        /// </summary>
        public const string CLAVE_DENEGAR = "DENEGAR";
        /// <summary>
        /// Clave CLAVE_DESCRIPCION_PIN.
        /// </summary>
        public const string CLAVE_DESCRIPCION_PIN = "CLAVE_DESCRIPCION_PIN";
        /// <summary>
        /// Clave CLAVE_DESCRIPCION_PIN_YA_GENERADO.
        /// </summary>
        public const string CLAVE_DESCRIPCION_PIN_YA_GENERADO = "CLAVE_DESCRIPCION_PIN_YA_GENERADO";
        /// <summary>
        /// Clave para el TEXTO_REGISTRATE
        /// </summary>
        public const string TEXTO_REGISTRATE = "TEXTO_REGISTRATE";
        /// <summary>
        /// Clave CLAVE_MENSAJE_PIN.
        /// </summary>
        public const string CLAVE_MENSAJE_PIN = "CLAVE_MENSAJE_PIN";
         /// <summary>
        /// Clave CLAVE_TITULO_ACCESO_PIN.
        /// </summary>
        public const string CLAVE_TITULO_ACCESO_PIN = "CLAVE_TITULO_ACCESO_PIN";

        private const string PARAMETRO_DOMINIO = "@dominio@";

        /// <summary>
        /// Parámetro PIN de GNOSS.
        /// </summary>
        public const string PARAMETRO_GNOSS_PIN = "gnoss_pin";

        #endregion


        #region Métodos generales


        /// <summary>
        /// Obtiene una cadena de texto según su clave.
        /// </summary>
        /// <param name="pClave">Clave</param>
        /// <param name="pIdioma">Idioma</param>
        /// <returns>Cadena de texto según su clave</returns>
        public static string ObtenerTexto(string pClave, string pIdioma)
        {
            string texto = "";

            if (pIdioma.Equals("en"))
            {
                switch (pClave)
                {
                    case CLAVE_DENEGADO:
                        texto = "Access denied. ";
                        break;
                    case CLAVE_DESCRIPCION:
                        texto = "<p><a href=\"http://ideas4all.com\">Ideas4all</a> request your permission to connect with your personal profile on <a href=\"http://gnoss.com\">gnoss.com</a>. If you accept, Ideas4all users can view your gnoss.com activity via a widget inserted into your Ideas4all profile.</p>";
                        break;
                    case TEXTO_REGISTRATE:
                        texto = "<p>If you aren’t a gnoss.com user yet, <a href=\"http://gnoss.com/en/register-user\">you can sign up now</a>.</p>";
                        break;
                    case CLAVE_DESCRIPCION_PIN:
                        //texto = "<p><a href=\"@1@\">@2@</a> request your permission to connect with your personal profile on <a href=\"http://gnoss.com\">gnoss.com</a>. If you accept, @2@ users can view your gnoss.com activity via a widget inserted into your @2@ profile.</p><p>Una vez aceptada la solicitud, se le proporcionará el PIN necesario para completar la conexión con la aplicación. Deberá copiarlo en su aplicación.</p><p>If you aren’t a gnoss.com user yet, <a href=\"http://gnoss.com/en/register-user\">you can sign up now</a>.</p>";
                        texto = "<p>if you accept, We provide you a PIN to complete de conexion with application. You must to copy it in yout application</p><p>The previous access tokens for this application are invalidated after entering a PIN in the application.</p>";
                        break;
                    case CLAVE_DESCRIPCION_PIN_YA_GENERADO:
                        //texto = "<p><a href=\"@1@\">@2@</a> request your permission to connect with your personal profile on <a href=\"http://gnoss.com\">gnoss.com</a>. If you accept, @2@ users can view your gnoss.com activity via a widget inserted into your @2@ profile.</p><p>Una vez aceptada la solicitud, se le proporcionará el PIN necesario para completar la conexión con la aplicación. Deberá copiarlo en su aplicación.</p><p>If you aren’t a gnoss.com user yet, <a href=\"http://gnoss.com/en/register-user\">you can sign up now</a>.</p><br/><br/><p>Ya has solicitado un PIN para esta aplicación. Si obtienes otro nuevo, el anterior quedará invalido.</p>";
                        texto = "<p>You've requested a PIN for this application. If you get a new one, the former will be invalid.</p>";
                        break;
                    case CLAVE_ENTRAR:
                        texto = "Sign in";
                        break;
                    case CLAVE_ERROR_LOGIN:
                        texto = "You must enter username and password";
                        break;
                    case CLAVE_NOMBRE:
                        texto = "Username or email";
                        break;
                    case CLAVE_PASSWORD:
                        texto = "Password";
                        break;
                    case CLAVE_TITULO_LOGIN:
                        texto = "Sign in";
                        break;
                    case CLAVE_PERIMITIR:
                        texto = "Allow";
                        break;
                    case CLAVE_DENEGAR:
                        texto = "Deny";
                        break;
                    case CLAVE_TITULO_ACCESO:
                        texto = "Ideas4all wants to connect with your gnoss.com user account.";
                        break;
                    case CLAVE_TITULO_ACCESO_PIN:
                        texto = "@1@ wants to connect with your gnoss.com user account.";
                        break;
                    case CLAVE_MENSAJE_PIN:
                        texto = "Acceso concedido. Copie el siguiente PIN, el cual deberá introducir en su aplicación para completar el proceso:";
                        break;
                }
            }
            else
            {
                switch (pClave)
                {
                    case CLAVE_DENEGADO:
                        texto = "Acceso denegado. ";
                        break;
                    case CLAVE_DESCRIPCION:
                        //Ideas4all 
                        //El sitio web Ideas4all solicita tu permiso para conectar tu perfil GNOSS 
                        texto = "<p>El sitio web <a href=\"http://ideas4all.com\">Ideas4all</a> solicita tu permiso para conectar tu perfil <a href=\"http://gnoss.com\">GNOSS</a> con tu perfil Ideas4all.</p>";
                        break;
                    case CLAVE_DESCRIPCION_PIN:
                        //texto = "<p><a href=\"@1@\">@2@</a> solicita tu permiso para conectar con tu perfil personal en <a href=\"http://gnoss.com\">gnoss.com</a>. Si aceptas, sus usuarios podrán contemplar tu actividad en gnoss.com a través de un widget ubicado en el perfil que has completado en @2@.</p><p>Una vez aceptada la solicitud, se le proporcionará el PIN necesario para completar la conexión con la aplicación. Deberá copiarlo en su aplicación.</p>";
                        texto = "<p>Una vez aceptada la solicitud, se le proporcionará el PIN necesario para completar la conexión con la aplicación. Deberá copiarlo en su aplicación.</p><p>Los tokens de acceso anteriores para esta aplicación se invalidarán una vez introducido el PIN en la aplicación.</p>";
                        break;
                    case TEXTO_REGISTRATE:
                        texto = "<p>Para poder autorizar el acceso a tu cuenta, primero debes acceder a ella.</p>";
                        break;
                    case CLAVE_DESCRIPCION_PIN_YA_GENERADO:
                        //texto = "<p><a href=\"@1@\">@2@</a> solicita tu permiso para conectar con tu perfil personal en <a href=\"http://gnoss.com\">gnoss.com</a>. Si aceptas, sus usuarios podrán contemplar tu actividad en gnoss.com a través de un widget ubicado en el perfil que has completado en @2@.</p><p>Una vez aceptada la solicitud, se le proporcionará el PIN necesario para completar la conexión con la aplicación. Deberá copiarlo en su aplicación.</p><p>En el caso de que no seas usuario de gnoss.com, <a href=\"http://gnoss.com/registrar-usuario\">puedes registrarte en este momento</a>.</p><br/><br/><p>Ya has solicitado un PIN para esta aplicación. Si obtienes otro nuevo, el anterior quedará invalido.</p>";
                        texto = "<p>Ya has solicitado un PIN para esta aplicación. Si obtienes otro nuevo, el anterior quedará invalido.</p>";
                        break;
                    case CLAVE_ENTRAR:
                        texto = "Entrar";
                        break;
                    case CLAVE_ERROR_LOGIN:
                        texto = "Debe introducir el usuario y la contraseña";
                        break;
                    case CLAVE_NOMBRE:
                        texto = "Nombre de usuario o email";
                        break;
                    case CLAVE_PASSWORD:
                        texto = "Contraseña";
                        break;
                    case CLAVE_TITULO_LOGIN:
                        texto = "Identifícate";
                        break;
                    case CLAVE_PERIMITIR:
                        texto = "Permitir";
                        break;
                    case CLAVE_DENEGAR:
                        texto = "Rechazar";
                        break;
                    case CLAVE_TITULO_ACCESO:
                        texto = "<a href=\"http://ideas4all.com\">Ideas4all</a> solicita autorización para acceder a tu cuenta de <a href=\"http://gnoss.com\">GNOSS</a>";
                        break;
                    case CLAVE_TITULO_ACCESO_PIN:
                        texto = "@1@ desea conectarse con tu cuenta de usuario en gnoss.com";
                        break;
                    case CLAVE_MENSAJE_PIN:
                        texto = "Acceso concedido. Copie el siguiente PIN, el cual deberá introducir en su aplicación para completar el proceso:";
                        break;
                }
            }

            return texto;
        }

        #endregion
    }
}
