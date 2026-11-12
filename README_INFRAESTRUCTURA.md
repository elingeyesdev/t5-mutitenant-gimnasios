# Gestión Gimnasios | Despliegue en Ubuntu Server con Docker

**Tarea 10: Implementación de infraestructura**

**Proyecto:** Gestión Gimnasios (aplicación web/API ASP.NET Core 10 y cliente .NET MAUI)

**Rama de trabajo:** `feature/infraestructura-ubuntu-docker`

**Estado:** web/API desplegada y autenticación funcional; acceso móvil, restauración y usuario Linux restringido pendientes de validación.

> Este documento describe **el laboratorio ejecutado**, no una instalación pública de producción. La base de datos es **Azure SQL Edge (compatible con T-SQL/SQL Server)** y está alojada en Docker Desktop del Mac. **No se implementó PostgreSQL ni una base de datos dentro del Compose de Ubuntu.** Estas diferencias frente a la rúbrica deben justificarse con el docente.

## 1. Arquitectura y requisitos

```text
MacBook Pro M2 Pro (macOS)
├─ VirtualBox 7.2.4
│  └─ Ubuntu Server 24.04.5 LTS ARM64  [VM: UbuntuServer-GestionGimnasios]
│     Hostname: gym-server  |  VM NAT: 10.0.2.15/24
│     ├─ SSH TCP/22  ← NAT 127.0.0.1:2222
│     ├─ UFW: 22/tcp, 80/tcp; denegar conexiones entrantes restantes
│     └─ Docker Engine 29.9.3 + Docker Compose 5.7.1
│        ├─ nginx:stable-alpine       :80     ← NAT 127.0.0.1:8081
│        └─ gestion-gimnasios-web:1.0 :8080   [solo red interna de Compose]
└─ Docker Desktop
   └─ sqlserver2022 (imagen mcr.microsoft.com/azure-sql-edge)
      TCP 1433; accesible desde Ubuntu por 10.0.2.2:1433
```

**Recursos asignados a la VM:** 2 vCPU, 4096 MB RAM, 40 GB disco dinámico, EFI, adaptador NAT; Ubuntu Server sin escritorio. Elección justificada por un laboratorio con una sola instancia web/API y Nginx, compilación y despliegue de .NET ARM64; la base de datos permanece en el host para evitar una instancia adicional de alto consumo de RAM y por restricciones de soporte ARM64 de la imagen estándar SQL Server. El dimensionamiento no sustituye pruebas de carga.

**Acceso web:** `http://127.0.0.1:8081/` desde el Mac anfitrión. Es HTTP de laboratorio y **solo localhost del Mac** en la regla NAT. Para uso público se requieren DNS, TLS/HTTPS, endurecimiento adicional y otra arquitectura de red.

## 2. Estructura de los archivos versionados

```text
repo/
├── .dockerignore
├── .env.example                   # plantilla pública, SIN credenciales
├── compose.yaml                   # servicios web y nginx
├── nginx/default.conf             # proxy inverso, conserva Host y puerto
└── web/GestionGimnasios.Web/
    ├── Dockerfile                 # compilación multietapa de .NET 10
    ├── GestionGimnasios.Web.csproj
    └── ...                        # aplicación, API, vistas y migraciones
```

**Nunca subir `.env`, claves SSH privadas, contraseñas ni tokens.** El `.env` real reside únicamente en `/home/melanie/gestion-gimnasios/.env` en Ubuntu, con permisos `600`.

## 3. Preparación del servidor

1. Instalar Ubuntu Server 24.04.5 **ARM64** en VirtualBox con usuario `melanie` y hostname `gym-server`. Seleccionar OpenSSH Server. Confirmar con `hostnamectl`, `ip -br a` y `lsb_release -a` (lectura, sin privilegios).
2. Actualizar paquetes:
   ```bash
   sudo apt update
   sudo apt upgrade
   ```
   Requieren `sudo`; modifican la base de datos de paquetes y software instalado. En el laboratorio se aplazaron temporalmente 3 actualizaciones por el sistema de *phased updates*.
3. Configurar NAT en VirtualBox: `127.0.0.1:2222 → 10.0.2.15:22` (SSH) y `127.0.0.1:8081 → 10.0.2.15:80` (web). El puerto 8080 del Mac ya estaba ocupado por Java; por eso se eligió 8081.
4. Crear en el **Mac**, no en Ubuntu, una clave personal de SSH:
   ```bash
   ssh-keygen -t ed25519 -f ~/.ssh/gym_server_ed25519 -C "gym-server"
   cat ~/.ssh/gym_server_ed25519.pub | ssh -p 2222 melanie@127.0.0.1 'umask 077; mkdir -p ~/.ssh; cat >> ~/.ssh/authorized_keys'
   ssh -i ~/.ssh/gym_server_ed25519 -p 2222 melanie@127.0.0.1
   ```
   `ssh-keygen` escribe una clave privada protegida en el Mac y una pública; `cat ...pub | ssh` añade **solo la clave pública** al archivo `authorized_keys` del usuario Ubuntu. Nunca compartir la clave privada ni su *passphrase*.
5. Archivo de endurecimiento creado en Ubuntu: `/etc/ssh/sshd_config.d/01-gym-hardening.conf`:
   ```text
   PasswordAuthentication no
   KbdInteractiveAuthentication no
   PermitRootLogin no
   PubkeyAuthentication yes
   ```
   Validar con `sudo sshd -t` y `sudo sshd -T | grep -E '^(pubkeyauthentication|passwordauthentication|kbdinteractiveauthentication|permitrootlogin) '`. Aplicar con `sudo systemctl reload ssh`. **Abrir una segunda sesión SSH y probarla antes de cerrar la primera.** Se verificó que autenticación solo por contraseña se deniega (`Permission denied (publickey)`).
6. Configurar UFW **permitiendo SSH antes de activar**:
   ```bash
   sudo ufw allow 22/tcp comment 'SSH'
   sudo ufw show added
   sudo ufw enable
   sudo ufw allow 80/tcp comment 'HTTP Nginx'
   sudo ufw status verbose
   ```
   Comandos con `sudo`; editan reglas del firewall y pueden afectar conectividad. Configuración comprobada: `deny incoming`, `allow outgoing`, `deny routed`, con 22/tcp y 80/tcp permitidos. **Nota:** puertos publicados por Docker pueden tener interacción especial con UFW; controlar además la exposición en Compose/NAT.
7. Antes de Docker se creó el snapshot de VirtualBox `Ubuntu-Base-SSH-Seguro` con la VM apagada. **No equivale a una restauración probada ni a un respaldo actualizado del despliegue.**

## 4. Instalación de Docker Engine y Compose (Ubuntu ARM64)

Usar repositorio oficial de Docker, no el daemon del Docker Desktop del Mac:

```bash
sudo apt install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
sudo tee /etc/apt/sources.list.d/docker.sources > /dev/null <<'DOCKER_REPO'
Types: deb
URIs: https://download.docker.com/linux/ubuntu
Suites: noble
Components: stable
Architectures: arm64
Signed-By: /etc/apt/keyrings/docker.asc
DOCKER_REPO
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo systemctl is-active docker
sudo docker run --rm hello-world
docker compose version
```

Los comandos anteriores requieren `sudo` para modificar `/etc/apt/`, instalar paquetes y usar el socket Docker. `docker compose version` no requiere privilegios. Se verificaron Engine 29.9.3, Compose 5.7.1 y `Hello from Docker!`. **No agregar indiscriminadamente usuarios al grupo `docker`**: equivale prácticamente a privilegios de administrador sobre el host.

## 5. Construcción, transferencia y configuración

En la **raíz del repositorio del Mac**:

```bash
dotnet build web/GestionGimnasios.Web/GestionGimnasios.Web.csproj -c Release
```

Se corrigieron avisos `NU1902`/`NU1903` de `System.Security.Cryptography.Pkcs` (transitiva de `Microsoft.Data.SqlClient`) agregando referencia directa a la versión 9.0.21. Segunda compilación Release exitosa en 4,4 s, sin advertencias.

Transferir solo el código web, el Dockerfile y `.dockerignore` desde el Mac (sin credenciales, binarios ni `movil/`):

```bash
rsync -avzR -e "ssh -i $HOME/.ssh/gym_server_ed25519 -p 2222" \
  --exclude='.git/' --exclude='bin/' --exclude='obj/' \
  --exclude='.env*' --exclude='appsettings.Development.json' \
  --exclude='appsettings.Local.json' --exclude='appsettings.Production.json' \
  --exclude='appsettings.*.local.json' --exclude='wwwroot/uploads/' \
  --exclude='*.pfx' --exclude='*.pem' --exclude='*.key' \
  .dockerignore ./web/GestionGimnasios.Web \
  melanie@127.0.0.1:~/gestion-gimnasios/
```

`-R` preserva la ruta `web/GestionGimnasios.Web`. Antes de la transferencia real se probó con `-n` (*dry-run*). Copiar también `compose.yaml` y `nginx/default.conf` desde el repositorio del Mac si se está **reproduciendo desde cero** (en la primera implementación se generaron en Ubuntu y se recuperaron con `scp`). Ejemplo:

```bash
scp -i ~/.ssh/gym_server_ed25519 -P 2222 compose.yaml melanie@127.0.0.1:~/gestion-gimnasios/compose.yaml
scp -i ~/.ssh/gym_server_ed25519 -P 2222 nginx/default.conf melanie@127.0.0.1:~/gestion-gimnasios/nginx/default.conf
```

En **Ubuntu**:

```bash
cd ~/gestion-gimnasios
sudo docker build -t gestion-gimnasios-web:1.0 -f web/GestionGimnasios.Web/Dockerfile .
sudo docker image ls gestion-gimnasios-web
```

Imagen generada exitosamente en 118,6 s, ID local `af9bce8100b0`. Si se cambia el código, **volver a transferirlo y reconstruir**; `docker compose up -d` por sí solo no reconstruye una imagen etiquetada existente.

## 6. Base de datos y archivo privado `.env`

La base de datos ejecutada en el Mac es un contenedor llamado `sqlserver2022`, imagen `mcr.microsoft.com/azure-sql-edge`, puerto `0.0.0.0:1433->1433/tcp`. Desde Ubuntu se usa `10.0.2.2:1433` gracias al NAT de VirtualBox.

**Incidente de red:** inicialmente `curl telnet://10.0.2.2:1433` respondió `Network is unreachable`. Se comprobó `ip route`, `ip route get 10.0.2.2`, `ping -c 4 10.0.2.2`, `docker ps` y `nc -vz -G 5 127.0.0.1 1433` en el Mac. Se identificó `LocalhostReachable: 0`. Con la VM **apagada**, se ejecutó en el Mac:

```bash
VBoxManage modifyvm "UbuntuServer-GestionGimnasios" --nat-localhostreachable1 on
VBoxManage showvminfo "UbuntuServer-GestionGimnasios" --details | grep -i localhost
```

Verificado `LocalhostReachable: 1` y posteriormente `Puerto SQL accesible` en Ubuntu mediante la prueba TCP. **Esta configuración concede a la VM acceso adicional a servicios locales del Mac: usarla solo en un laboratorio confiable.**

Crear `/home/melanie/gestion-gimnasios/.env` en **Ubuntu** con permisos 600:

```bash
install -m 600 /dev/null ~/gestion-gimnasios/.env
nano ~/gestion-gimnasios/.env
```

Contenido **de ejemplo** (reemplazar valores localmente, nunca pegarlos en chats ni commitearlos):

```dotenv
SQL_CONNECTION_STRING='Server=10.0.2.2,1433;Database=GestionGimnasios;User Id=sa;Password=CAMBIAR_CONTRASENA;Encrypt=True;TrustServerCertificate=True'
JWT_CLAVE='CAMBIAR_POR_CLAVE_ALEATORIA_SEGURA'
```

La clave JWT se generó en Ubuntu mediante Python `secrets.token_urlsafe(48)` y se guardó en `.env` sin imprimirla. Evitar contraseñas con comillas simples sin escapado apropiado en archivos dotenv. `TrustServerCertificate=True` solo es admisible aquí como concesión de laboratorio; en producción configurar certificados y validarlos.

**IMPORTANTE:** aunque el archivo `.env` tenga permisos `600`, los valores interpolados por Docker Compose se incorporan al entorno de los contenedores. Tratar el acceso al socket Docker y las inspecciones de contenedores como acceso privilegiado a secretos. Evitar mostrar `docker compose config` sin `--quiet`.

## 7. Arrancar y operar los servicios

```bash
cd ~/gestion-gimnasios
sudo docker compose config --quiet
sudo docker compose up -d
sudo docker compose ps
curl -s -o /dev/null -w 'HTTP %{http_code}\n' http://127.0.0.1/Cuenta/Login
curl -sSI -H 'Host: 127.0.0.1:8081' http://127.0.0.1/
```

Resultado observado: Nginx y web en estado `Up`, login HTTP `200`, raíz HTTP `302` con `Location: http://127.0.0.1:8081/Cuenta/Login?ReturnUrl=%2F`. Inicio de sesión real como usuario de prueba y panel de resumen con datos confirmados desde Chrome. No publicar ni registrar las credenciales del usuario de prueba.

**Revisión de registros (todavía pendiente de registrar como evidencia):**

```bash
sudo docker compose logs --tail=100 web
sudo docker compose logs --tail=100 nginx
sudo journalctl -u docker -n 50 --no-pager
sudo ufw status verbose
```

Los logs pueden contener información personal, tokens o cadenas de consulta; revisar y censurar antes de adjuntarlos.

**Apagar/encender los servicios manualmente:**

```bash
sudo docker compose stop             # detiene servicios sin borrarlos
sudo docker compose start            # arranca servicios existentes
sudo docker compose up -d            # crea/reconcilia servicios según compose.yaml
sudo docker compose ps               # comprueba estado
```

`restart: unless-stopped` está configurado para ambos servicios. Se ejecutó `sudo reboot`, se reconectó por SSH y se confirmó que los dos contenedores volvieron automáticamente a `Up`; el endpoint raíz siguió devolviendo HTTP 302 correcto. Evitar `docker compose down -v` porque elimina volúmenes definidos, si existieran. Para apagar el laboratorio: `sudo poweroff`, esperar `Powered Off`, cerrar VirtualBox y opcionalmente Docker Desktop del Mac.

## 8. Incidente de redirección (Nginx)

Al abrir `http://127.0.0.1:8081/`, ASP.NET Core generaba un `Location: http://127.0.0.1/Cuenta/Login...` **sin el puerto**, llegando a Laravel Herd del Mac. En `nginx/default.conf` se cambió:

```nginx
proxy_set_header Host $host;
# POR:
proxy_set_header Host $http_host;
```

Se validó `nginx -t`; una simple recarga no aplicó el cambio porque el bind mount seguía mostrando el archivo antiguo. Se recreó **solo Nginx**:

```bash
sudo docker compose up -d --no-deps --force-recreate nginx
sudo docker compose exec nginx grep 'proxy_set_header Host' /etc/nginx/conf.d/default.conf
curl -sSI -H 'Host: 127.0.0.1:8081' http://127.0.0.1/
```

Evidencia final: `$http_host` activo, redirección con `:8081` y login visible desde Chrome. La recreación es relevante porque el archivo se montó con ruta de archivo individual.

## 9. Evidencias y puntos pendientes

**Validado:** SSH con clave y rechazo de contraseña; UFW activo; Docker Engine/Compose y `hello-world`; compilación local y construcción Docker ARM64; conectividad TCP a SQL Server; login HTTP 200; autenticación y lectura de datos; redirección correcta; reinicio automático tras `sudo reboot`; estado de Git en rama de infraestructura.

**PENDIENTE según rúbrica:**

- Acceso de la aplicación **MAUI** al servidor de Ubuntu desde emulador o dispositivo y captura de respuesta API. En `movil/GestionGimnasios.Movil/Servicios/Sesion.cs` se detectó `ServidorPorDefecto = "http://10.0.2.2:5253"` y dirección editable por `Preferences`; para Android Emulator `10.0.2.2` es el host del **emulador**, no directamente la VM. Habrá que confirmar la URL correcta según el entorno y probar login/endpoint.
- Crear **usuario Linux específico de mínimos privilegios**, demostrar accesos permitidos/denegados y documentar.
- Crear un **backup y demostrar una restauración real** con evidencia antes/después. El snapshot `Ubuntu-Base-SSH-Seguro` se creó, pero **no se restauró**. El `sudo reboot` solo demuestra recuperación automática.
- Revisar y conservar evidencia **de logs**, con datos sensibles redactados.
- Comprobar que **otro estudiante** pueda seguir efectivamente este README; no está validado.
- **PuTTY** aparece en la rúbrica; se usó OpenSSH nativo en macOS. Confirmar si acepta equivalencia.
- La rúbrica menciona **PostgreSQL y BD en Docker Compose**: el laboratorio conserva Azure SQL Edge alojado en Docker Desktop del Mac. Obtener aceptación expresa del docente o adaptarlo.

## 10. Seguridad y límites

- Mantener SSH con claves ED25519 y contraseña de usuario separada para `sudo`; nunca subir `~/.ssh/gym_server_ed25519`.
- Las reglas NAT de SSH y HTTP escuchan solo en `127.0.0.1` del Mac. El servicio dentro de Ubuntu no está preparado como producción pública.
- `compose.yaml` publica `80:80` **dentro de Ubuntu**; verificar exposición antes de cambiar NAT o a modo puente.
- La contraseña del usuario `sa` ofrece privilegios altos sobre la base de datos; preferir un usuario de aplicación con mínimos permisos en una iteración posterior.
- El cliente móvil y el backend HTTP requieren protección HTTPS y tratamiento correcto de cookies/tokens si se exponen fuera del laboratorio.
- No incluir capturas de `.env`, valores de `docker inspect` con secretos, ni contraseñas de cuentas de prueba.

## 11. Registro de uso de IA y validación por el estudiante

| Consulta/uso de IA | Decisión humana y verificación |
|---|---|
| Instalación Ubuntu ARM64, VirtualBox, SSH y UFW | Se ejecutaron pasos con control del usuario; `sshd -t`, nueva sesión SSH, `ufw status verbose` y prueba de contraseña denegada. |
| Docker y .NET 10 ARM64 | Se revisó el Dockerfile, se compiló Release y se ejecutó `hello-world`; imagen construida con 15/15 pasos exitosos. |
| Transferencia de archivos | La primera simulación `rsync -n` reveló pérdida de directorios; se corrigió con `-R`, se hizo otra simulación y luego copia real. |
| Diagnóstico SQL Server | Se verificaron rutas, ping, puerto de Docker Desktop y `LocalhostReachable`; se corrigió únicamente la opción NAT. |
| Diagnóstico redirección Nginx | Se reprodujo HTTP 302 sin puerto; se corrigió `$host` por `$http_host`; se comprobó configuración **dentro** del contenedor y respuesta final. |
| Seguridad NuGet | Se identificó dependencia vulnerable con `dotnet nuget why`; se fijó una versión corregida y se compiló sin avisos. |
| Documentación | Borrador estructurado con ayuda de IA, pendiente de revisión y reproducibilidad por otro estudiante. |

El estudiante debe **comprender cada comando**, revisar rutas y privilegios, adaptar los valores de ejemplo y registrar evidencias reales. Los comandos con `sudo`, `docker`, `VBoxManage` o edición de `/etc/` pueden cambiar la seguridad o disponibilidad del sistema y no deben ejecutarse indiscriminadamente.

---

**Referencia complementaria:** `docs/INFRAESTRUCTURA_TAREA10.md` contiene inventario, bitácora técnica, matriz de verificación, detalles de incidentes y guía de capturas para presentar al docente.
