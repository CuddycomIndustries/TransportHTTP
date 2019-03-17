## Overview
Faction supports a "plug and play" like framework for Transports, this project is a simple HTTP Server written in .NET Core configured to run as a Docker Container, the accompanying Marauder module can be found [here]().

## Getting Started
To get started, ensure you have Docker installed on a host you intend to serve as the Http Transport, then git pull this repo.

```
git pull https://github.com/FactionC2/TransportHTTP.git
```

If you want to use a valid SSL Certificate, create the certificate and if necessary convert it to a .pfx file named cert.pfx, otherwise the installation will create a self signed SSL certificate.

To convert from openssl to .pfx:
```
openssl pkcs12 -export -out cert.pfx -inkey privateKey.key -in certificate.crt
```

Then create a new Transport within the Faction Console as documented [here](https://github.com/FactionC2/Docs/blob/master/docs/README.md). Update the `config.json` file with the correct `Id`, `Key`, and `Secret` as well as the URL of the Faction API service and the HttpTransport Server (this servers) DNS or IP.

Finally, build and run the Docker container. 
```
docker build -t "transporthttp" .

docker run -d -p 443:443 -e "FACTION_PFX_FILE=./opt/faction/cert.pfx" -e "FACTION_PFX_SECRET=Sup3rS3cr3t" transporthttp
```

If using a custom SSL certificate, you can override the default SSL Certificate Path and Secret within Docker by overriding the enviornment variables `FACTION_PFX_FILE` &
`FACTION_PFX_SECRET` as such:
```
docker run -p 443:443 transporthttp
```

---

## Customization
The transport is designed to support basic customization of how the Http Server and Marauder Http Module communicate with each other. Defined within the `/Profile/HTTPProfile.cs` are `ClientProfile` and `ServerProfile` methods. Both profiles define how each component (Server and Module) will shape their Http requests and responses.
The ClientProfile is seralized and sent to the Faction API endpoint at startup during the Transports registration process. These profile settings are then injected into the Marauder transport module during payload build. 

### Server Customization
Within the Server Profile configuration are the `HttpGet` and `HttpPost` profiles which define how this server will respond to Marauder requests.

#### URL
The `URL` section defines what URL endpoint's are supported for the request type (Get or Post) as well as what the content of the response will be. The HTML or Javascript is also directly defined in the configruation, in the case of the `HttpGet` section, the HTML content can contain variables (enclosed like %%this%%) as a placeholder for the payload content to be injected.

#### Headers
The `Headers` section define static headers that will be included in each request/response.

#### Payload
Finally the `Payload` section define where the Faction Message payload will be located within the content, supported locations are `Cookie`, `Header`, or `Body`. In the case of `Body` the variable defined must match the variable defined in the HTML or Javascript content in the `URL` section. 

### Client Customization
The design and configuration of the Client Profile is the same as the Server Customization, execpt these settings control how the Marauder Http Transport module behaves for all request's and where the Server should look for client side payload content. This configuration is seralized and injected into Marauder when the Http Transport module is selected during Payload creation.