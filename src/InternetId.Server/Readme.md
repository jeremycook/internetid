# InternetId.Server

## OpenIddict certificates

The identity provider loads two certificates and their corresponding keys in PEM format from configuration. ENCRYPTION_CERTIFICATE and ENCRYPTION_KEY as well as SIGNING_CERTIFICATE and SIGNING_KEY are the names of the configuration values.

The IIS Express development environment has snake oil values that are used by default when developing in that way.

OpenSSL can be used to generate the encryption and signing certificates and keys for production, test and other non-development environments. Those values can be provided as environment variables or any other way that is reasonable.

```bash
# Generate an encryption certificate
openssl req -x509 -sha256 -newkey rsa:4096 -nodes -days 3650 -subj "/C=/ST=/L=/O=/CN=OpenIddict Server Encryption Certificate" -keyout ENCRYPTION_KEY -out ENCRYPTION_CERTIFICATE

# Generate an signing certificate
openssl req -x509 -sha256 -newkey rsa:4096 -nodes -days 3650 -subj "/C=/ST=/L=/O=/CN=OpenIddict Server Signing Certificate" -keyout SIGNING_KEY -out SIGNING_CERTIFICATE
```
