![](https://content.gnoss.ws/imagenes/proyectos/personalizacion/7e72bf14-28b9-4beb-82f8-e32a3b49d9d3/cms/logognossazulprincipal.png)

# Gnoss.Web.OAuth.OpenCORE

![](https://github.com/equipognoss/Gnoss.Web.OAuth.OpenCORE/workflows/BuildOAuth/badge.svg)

Aplicación Web que se encarga de validar las firmas Oauth que le llegan a la Web o el API. Esta aplicación NO debe ser accesible desde el exterior de la plataforma GNOSS, sólo debe estar disponible para que el resto de aplicaciones puedan hacer peticiones Web a ella.

Configuración estandar de esta aplicación en el archivo docker-compose.yml: 

```yml
oauth:
    image: gnoss/gnoss.web.oauth.opencore
    env_file: .env
    ports:
     - ${puerto_oauth}:80
    environment:
     acid: ${acid}
     base: ${base}
     oauth: ${oauth}
     virtuosoConnectionString: ${virtuosoConnectionString}
     redis__redis__ip__master: ${redis__redis__ip__master}
     redis__redis__ip__read: ${redis__redis__ip__read}
     redis__redis__bd: ${redis__redis__bd}
     redis__redis__timeout: ${redis__redis__timeout}
     redis__recursos__ip__master: ${redis__recursos__ip__master}
     redis__recursos__ip__read: ${redis__recursos__ip__read}
     redis__recursos__bd: ${redis__recursos__bd}
     redis__recursos__timeout: ${redis__redis__timeout}
     RabbitMQ__colaReplicacion: ${RabbitMQ}
     RabbitMQ__colaServiciosWin: ${RabbitMQ}
     idiomas: ${idiomas}
     Servicios__urlLogin: ${Servicios__urlLogin}
     connectionType: ${connectionType}
    volumes:
      - ./logs/oauth:/app/logs
```

Se pueden consultar los posibles valores de configuración de cada parámetro aquí: https://github.com/equipognoss/Gnoss.SemanticAIPlatform.OpenCORE

## Código de conducta
Este proyecto a adoptado el código de conducta definido por "Contributor Covenant" para definir el comportamiento esperado en las contribuciones a este proyecto. Para más información ver https://www.contributor-covenant.org/

## Licencia
Este producto es parte de la plataforma [Gnoss Semantic AI Platform Open Core](https://github.com/equipognoss/Gnoss.SemanticAIPlatform.OpenCORE), es un producto open source y está licenciado bajo GPLv3.
