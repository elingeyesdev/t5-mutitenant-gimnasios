# Informe técnico de infraestructura: Gestión Gimnasios

**Asignatura:** Proyectos de Sistemas III · Tarea 10, Implementación de infraestructura

**Fecha de ejecución documentada:** 11 y 12 de noviembre de 2026 (hora local Bolivia / UTC en Ubuntu)

**Responsable:** Melanie Díaz Otero

**Proyecto:** Sistema multi-tenant de gestión de gimnasios

**Rama:** `feature/infraestructura-ubuntu-docker`

**Commit de infraestructura registrado:** `0af48fb` (`Configurar despliegue Docker y Nginx en Ubuntu Server`)

**Estado del documento:** informe de avances verificables; pendientes identificados al final.

## 1. Objetivo, alcance y decisión arquitectónica

**Objetivo de la tarea:** instalar y administrar un servidor Linux exclusivamente por terminal; desplegar una solución en contenedores, comunicarla con una base de datos, proteger accesos, obtener evidencias y comprobar recuperación.

**Alcance realmente implementado:** Ubuntu Server 24.04.5 ARM64 sin escritorio en VirtualBox 7.2.4; SSH por clave; UFW; Docker Engine 29.9.3; Docker Compose 5.7.1; contenedor ASP.NET Core 10 (web y endpoints de API); contenedor Nginx; SQL Server compatible (Azure SQL Edge) **externo a Ubuntu**, funcionando en Docker Desktop en el host Mac; página web accesible desde navegador del host con autenticación y lectura de datos.

**Diferencia frente al enunciado:** la verificación menciona PostgreSQL y base de datos desplegada con Compose, pero el proyecto asignado estaba construido con Entity Framework Core SQL Server y una base Azure SQL Edge ya utilizada en desarrollo. Para evitar sustituir el proveedor y arriesgar datos existentes en la fecha de entrega se mantuvo esta base como servicio externo. Esto se debe informar al docente y obtener su aceptación; **no afirmar que PostgreSQL está desplegado**.

### 1.1 Diagrama lógico

```text
[Chrome/Safari en Mac: http://127.0.0.1:8081]
                      |
             VirtualBox NAT TCP
   127.0.0.1:8081 -> 10.0.2.15:80
                      |
          [Ubuntu Server, 10.0.2.15]
             UFW 80/tcp y 22/tcp
                      |
          [Nginx Docker :80]           [SSH :22]
                      |                    ^
            red interna Compose           |
                      v                    |
          [ASP.NET Core 10 :8080]   127.0.0.1:2222 (Mac)
                      |
           10.0.2.2:1433 (NAT)
                      v
        [Azure SQL Edge, Docker Desktop Mac]
```

### 1.2 Recursos y dimensionamiento

| Recurso | Configuración usada | Justificación | Verificación |
|---|---|---|---|
| Plataforma | MacBook Pro con M2 Pro | Host disponible para virtualización local | VM ARM64 ejecutada |
| Hipervisor | VirtualBox 7.2.4 | VM independiente y snapshots | VM `UbuntuServer-GestionGimnasios` |
| Sistema operativo | Ubuntu Server 24.04.5 LTS ARM64, kernel 6.8 | Sin GUI, estable y compatible con Docker ARM64 | Banner SSH |
| CPU | 2 vCPU | Carga de laboratorio limitada; compilación posible | Build Docker finalizado |
| RAM | 4096 MB | Nginx + .NET y herramientas; BD fuera de la VM | 6–8% RAM en reposo (banner SSH) |
| Disco | 40 GB dinámico, raíz ext4 sobre LVM (~29.36 GB visible) | Sistema, imágenes y capas Docker | ~32% de `/` tras despliegue |
| Red | NAT `10.0.2.15/24`, puerta `10.0.2.2` | Aislamiento básico y port forwarding controlado | SSH, ping, HTTP, prueba TCP SQL |
| Puertos invitado | TCP 22, TCP 80 | SSH administrativo y Nginx HTTP | UFW activo |
| Puertos host | localhost 2222 y 8081 | Evitar exposición a otros dispositivos | Reglas NAT verificadas |
| GPU | No requerida | Servidor HTTP/API y BD | No se asignó GPU |

**Limitación:** la asignación es apropiada para demostración, pero no representa un cálculo de capacidad definitivo. Las pruebas de carga, métricas de uso bajo concurrencia y requisitos de almacenamiento a largo plazo **no se realizaron**.

## 2. Inventario de configuración y seguridad

| Elemento | Resultado verificado |
|---|---|
| Hostname | `gym-server` |
| Usuario Ubuntu administrador inicial | `melanie`, con sudo |
| Interfaz de red | `enp0s8` |
| IP invitado | `10.0.2.15/24` |
| Gateway NAT | `10.0.2.2` |
| SSH | OpenSSH Server, puerto 22, acceso por clave pública ED25519 |
| SSH root / contraseña | `PermitRootLogin no`, `PasswordAuthentication no` |
| SSH NAT Mac | `127.0.0.1:2222 -> 10.0.2.15:22` |
| UFW | `active`; `deny incoming`, `allow outgoing`, `deny routed`; permitir 22 y 80 TCP (v4/v6) |
| Docker Engine | 29.9.3 |
| Docker Compose | 5.7.1 |
| Imagen web | `gestion-gimnasios-web:1.0` (ID local `af9bce8100b0`) |
| Imagen proxy | `nginx:stable-alpine` (Nginx 1.30.6 visto en respuesta) |
| Contenedores | `gestion-gimnasios-web-1`, `gestion-gimnasios-nginx-1` |
| Red de Compose | `gestion-gimnasios_interna` |
| BD | Azure SQL Edge en contenedor Docker Desktop `sqlserver2022` del Mac |
| Conexión del servidor a BD | `10.0.2.2:1433`, TCP probado |
| Nginx NAT Mac | `127.0.0.1:8081 -> 10.0.2.15:80` |
| Carpeta de despliegue | `/home/melanie/gestion-gimnasios/` |
| Archivo de secretos | `/home/melanie/gestion-gimnasios/.env`, permisos `600` |
| Snapshot | `Ubuntu-Base-SSH-Seguro`, previo a Docker |

### 2.1 Reglas relevantes de seguridad

- SSH público por clave, con rechazo explícito de contraseña y acceso root.
- Redirecciones NAT restringidas a `127.0.0.1` en macOS. No se usó acceso por IP pública.
- Aplicación web sin puerto `8080` publicado al exterior de Docker Compose; Nginx expone el puerto HTTP del invitado.
- Proceso .NET ejecutado con `USER $APP_UID` dentro de la imagen runtime.
- `UFW` activado solo después de registrar `allow 22/tcp`; habilitado `80/tcp` para Nginx.
- Secretos en `.env` privado, no en `.env.example` ni en Git. *Advertencia*: Docker Engine y los metadatos de contenedores pueden exponer variables a administradores del host.
- Inicialmente se habilitó `LocalhostReachable` para conectar VM y BD del Mac; concesión de laboratorio con riesgo documentado.
- **Pendiente:** cuenta Linux específica con mínimos privilegios (distinta del administrador) y verificación de accesos.

## 3. Bitácora técnica de comandos, efectos y privilegios

**Leyenda:** `Mac` = terminal local macOS; `VM` = terminal Ubuntu mediante SSH; `sudo` = requiere derechos administrativos; `lectura` = comando sin cambio persistente. Las líneas con valores de ejemplo no contienen credenciales.

### Fase A: instalación, red y diagnóstico de Ubuntu

| Comando / acción | Equipo / privilegios | Qué hace y qué modifica | Evidencia o resultado |
|---|---|---|---|
| Instalar ISO `ubuntu-24.04.5-live-server-arm64.iso` mediante asistente VirtualBox | GUI VirtualBox / host | Crea VDI, particiones Ubuntu, usuario, hostname y SSH | `gym-server`, Ubuntu 24.04.5 |
| `ip -br addr` / `ip route` | VM / lectura | Inspecciona interfaces, IPs y tabla de rutas | `enp0s8 10.0.2.15`, default `10.0.2.2` |
| `sudo apt update` | VM / sudo | Actualiza índices de paquetes en `/var/lib/apt/lists` | Completado |
| `sudo apt upgrade` | VM / sudo | Actualiza paquetes instalados | 27 actualizaciones; 3 aplazadas |
| `sudo systemctl is-system-running` | VM / sudo no indispensable | Estado global de systemd | `running` |
| `sudo systemctl is-active ssh` y `sudo systemctl is-active ssh.socket` | VM / lectura | Verifica servicio y socket SSH | Socket activo; servicio inicialmente `inactive` por activación bajo demanda |
| Regla NAT `127.0.0.1:2222 -> 10.0.2.15:22` | VirtualBox host | Modifica configuración de red de la VM | SSH desde Mac exitoso |

**Incidencia durante instalación:** al reiniciar el instalador apareció `Failed unmounting cdrom.mount`; después de finalizar instalación se apagó la VM, se retiró el ISO y el sistema arrancó desde el disco virtual. No hubo que reinstalar Ubuntu.

### Fase B: autenticación SSH y restricciones

| Comando | Equipo / privilegios | Efecto y archivos | Validación |
|---|---|---|---|
| `ssh-keygen -t ed25519 -f ~/.ssh/gym_server_ed25519 -C "gym-server"` | Mac / usuario | Crea clave privada y `.pub` en `~/.ssh` del Mac | Solicitud de passphrase y acceso posterior |
| `cat ~/.ssh/gym_server_ed25519.pub \| ssh -p 2222 melanie@127.0.0.1 'umask 077; mkdir -p ~/.ssh; cat >> ~/.ssh/authorized_keys'` | Mac + VM / usuario | Añade solo clave pública a `~/.ssh/authorized_keys` en Ubuntu | Login con clave exitoso |
| `ssh -i ~/.ssh/gym_server_ed25519 -p 2222 melanie@127.0.0.1` | Mac / usuario | Crea sesión remota cifrada | Prompt `melanie@gym-server` |
| `sudo tee /etc/ssh/sshd_config.d/01-gym-hardening.conf` con opciones de endurecimiento | VM / sudo | Crea fragmento de configuración SSH en `/etc/ssh/sshd_config.d/` | Valores efectivos comprobados |
| `sudo sshd -t` | VM / sudo | Verifica sintaxis sin aplicar cambios | Sin error |
| `sudo sshd -T \| grep -E ...` | VM / sudo | Muestra configuración efectiva | root `no`, password `no`, pubkey `yes` |
| `sudo systemctl reload ssh` | VM / sudo | Recarga configuración sin reiniciar Ubuntu | Nueva sesión de clave exitosa |
| `ssh -o PubkeyAuthentication=no -o PreferredAuthentications=password,keyboard-interactive -p 2222 melanie@127.0.0.1` | Mac / usuario | Fuerza intento sin clave para probar rechazo | `Permission denied (publickey)` |
| `sudo poweroff` | VM / sudo | Detiene servidor de forma segura | VM `Powered Off` |
| Snapshot `Ubuntu-Base-SSH-Seguro` | VirtualBox host | Punto de restauración de VM previa a Docker | Snapshot visible en VirtualBox |

La primera conexión SSH se comprobó comparando la huella de la clave de host ED25519 del servidor antes de aceptarla en el Mac. No se compartieron claves privadas ni contraseñas.

### Fase C: UFW y control de puertos

| Comando | Equipo / privilegios | Efecto | Verificación |
|---|---|---|---|
| `sudo ufw status verbose` | VM / sudo | Lee política actual | Inicialmente `inactive` |
| `sudo ufw allow 22/tcp comment 'SSH'` | VM / sudo | Agrega regla SSH IPv4/IPv6 | `Rules updated` |
| `sudo ufw show added` | VM / sudo | Lee reglas registradas | 22/tcp confirmado |
| `sudo ufw enable` | VM / sudo | Activa filtrado y persistencia en arranque | `active and enabled on system startup` |
| `sudo ufw allow 80/tcp comment 'HTTP Nginx'` | VM / sudo | Permite HTTP desde red de invitado | IPv4/IPv6 registrados |
| `sudo ufw status verbose` | VM / sudo | Revisa defaults y reglas efectivas | `deny incoming`, 22/80 |
| Segunda sesión `ssh ... -p 2222` | Mac / usuario | Prueba acceso con firewall activo | Login exitoso |
| `sudo ss -ltnp \| grep ':80 '` | VM / sudo | Inspecciona procesos escuchando HTTP | Sin procesos antes de Nginx |

**Precaución:** habilitar UFW sin regla SSH puede dejar al administrador sin acceso. Docker puede insertar reglas propias en el firewall; limitar la publicación de puertos en la capa de NAT/Compose y no extrapolar la prueba a un servidor expuesto a Internet.

### Fase D: Docker Engine y prueba de imagen

| Comando | Equipo / privilegios | Efecto / archivos | Verificación |
|---|---|---|---|
| `docker --version` | VM / lectura | Detecta instalación previa | `command not found` inicialmente |
| `sudo apt install -y ca-certificates curl` | VM / sudo | Instala herramientas de validación TLS y descarga | Ya instaladas |
| `sudo install -m 0755 -d /etc/apt/keyrings` | VM / sudo | Crea directorio para claves APT | Sin errores |
| `sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc` | VM / sudo | Descarga clave pública de firma | Completado |
| `sudo chmod a+r /etc/apt/keyrings/docker.asc` | VM / sudo | Habilita lectura de clave pública | Completado |
| `sudo tee /etc/apt/sources.list.d/docker.sources` + contenido `noble stable arm64` | VM / sudo | Registra repositorio oficial de Docker | `apt update` detecta `download.docker.com` |
| `sudo apt update` | VM / sudo | Actualiza catálogos APT | Repositorio detectado |
| `sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin` | VM / sudo | Instala daemon, CLI, runtime y plugins; crea servicios systemd | Instalación de 7 paquetes exitosa |
| `sudo systemctl is-active docker` | VM / sudo | Estado del servicio Docker | `active` |
| `sudo docker run --rm hello-world` | VM / sudo | Descarga imagen ARM64, ejecuta contenedor efímero | `Hello from Docker!` |
| `docker compose version` | VM / lectura | Verifica plugin | `v5.7.1` |

### Fase E: revisión, actualización de seguridad y empaquetado .NET

| Comando | Equipo / privilegios | Efecto | Resultado |
|---|---|---|---|
| `ls -la web`, `ls -la web/GestionGimnasios.Web` | Mac / lectura | Inventario del proyecto | `GestionGimnasios.Web.csproj`, `Program.cs`, MVC/API, vistas |
| `cat web/GestionGimnasios.Web/GestionGimnasios.Web.csproj` | Mac / lectura | Confirma `net10.0` y EF Core SQL Server | Framework y paquetes identificados |
| `grep -nE 'ConnectionStrings\|UseSqlServer\|Migrate\|EnsureCreated\|GetConnectionString' .../Program.cs` | Mac / lectura | Localiza cadena de conexión y migraciones | `GetConnectionString("Default")`, `MigrateAsync()` |
| `grep -n -C 8 -E 'MigrarAlIniciar\|CargarDatosDePrueba\|MigrateAsync' .../Program.cs` | Mac / lectura | Consulta condiciones de arranque | Se optó por desactivar migraciones/semillas |
| `cat .gitignore`, `git status -sb` | Mac / lectura | Inspección de seguridad y rama | `.env` ignorado, rama feature |
| Crear `.dockerignore` y `web/GestionGimnasios.Web/Dockerfile` | Mac / usuario | Archivos de compilación multietapa, sin secretos locales | Contenido inspeccionado con `cat` |
| `dotnet build web/GestionGimnasios.Web/GestionGimnasios.Web.csproj -c Release` | Mac / usuario | Compila y restaura NuGet, genera `bin/obj` | Primer build 86.3 s, alertas NuGet |
| `dotnet nuget why ... System.Security.Cryptography.Pkcs` | Mac / usuario | Muestra dependencia transitiva | EF Core → SqlClient 6.1.6 → Pkcs 9.0.11 |
| `dotnet add ... package System.Security.Cryptography.Pkcs --version 9.0.21` | Mac / usuario | Modifica `.csproj` y restaura NuGet | Éxito |
| `dotnet build ... -c Release` | Mac / usuario | Recompila con versión corregida | 4.4 s, sin advertencias |
| `git status --short` | Mac / lectura | Inspecciona cambios sin modificar el repo | Tres archivos iniciales esperados |

**Dockerfile multietapa:** imagen `mcr.microsoft.com/dotnet/sdk:10.0` para restaurar, compilar y publicar; runtime `mcr.microsoft.com/dotnet/aspnet:10.0` y usuario no privilegiado `$APP_UID`, expuesto puerto 8080 solo dentro de la red Docker. No se incorporaron contraseñas a la imagen.

### Fase F: transferencia y build en Ubuntu

| Comando | Equipo / privilegios | Efecto | Resultado |
|---|---|---|---|
| `rsync --version` | Mac y VM / lectura | Compatibilidad de transferencia | Mac OpenRsync 2.6.9 compatible; Ubuntu 3.2.7 |
| `mkdir -p ~/gestion-gimnasios/web/GestionGimnasios.Web` | VM / usuario | Crea directorio de despliegue | Sin errores |
| `rsync -avzn ... .dockerignore web/GestionGimnasios.Web/ ...` | Mac / usuario | Simulación de copia por SSH | Detectó pérdida de estructura de carpetas |
| `rsync -avznR ... .dockerignore ./web/GestionGimnasios.Web ...` | Mac / usuario | Simula copia conservando rutas | 211 elementos con rutas correctas |
| `rsync -avzR ...` (sin `-n`) | Mac / usuario | Transfiere código web y Dockerfile por SSH | ~8.07 MB de contenido, sin errores |
| `ls -la ~/gestion-gimnasios/web/GestionGimnasios.Web/` | VM / lectura | Verifica estructura recibida | Dockerfile, csproj, Program.cs, carpetas |
| `cd ~/gestion-gimnasios` | VM / usuario | Cambia directorio de trabajo | Contexto de build correcto |
| `sudo docker build -t gestion-gimnasios-web:1.0 -f web/GestionGimnasios.Web/Dockerfile .` | VM / sudo | Descarga imágenes ARM64, construye, publica, guarda imagen | 15/15 pasos en 118.6 s |
| `sudo docker image ls gestion-gimnasios-web` | VM / sudo | Inspecciona imagen creada | ID `af9bce8100b0`, 500MB uso disco, 128MB contenido |

**Lección documentada:** al indicar un directorio como fuente a rsync sin `--relative`, su contenido llega directamente al destino. Usar `-R` o adecuar el destino antes de ejecutar una copia real. Se aplicó primero *dry-run*, conforme a las reglas del curso.

### Fase G: diagnóstico de Azure SQL Edge entre Mac y VM

| Comando | Equipo / privilegios | Propósito | Evidencia |
|---|---|---|---|
| `timeout 5 bash -c 'echo > /dev/tcp/10.0.2.2/1433' ...` | VM / usuario | Prueba TCP SQL | Inicialmente `Network is unreachable` |
| `ip route` | VM / lectura | Inspecciona gateway | Ruta hacia `10.0.2.2` presente |
| `ping -c 4 10.0.2.2` | VM / usuario | Prueba ICMP | 4/4, 0% pérdida |
| `docker ps --filter "name=sqlserver2022"` | Mac / acceso Docker | Comprueba contenedor y puertos | Azure SQL Edge, `0.0.0.0:1433->1433/tcp` |
| `nc -vz -G 5 127.0.0.1 1433` | Mac / usuario | Conectividad local SQL | Exitosa |
| `ip route get 10.0.2.2` | VM / lectura | Consulta ruta elegida | `dev enp0s8 src 10.0.2.15` |
| `curl -v --connect-timeout 5 telnet://10.0.2.2:1433` | VM / usuario | Diagnóstico TCP detallado | Inicialmente `Network is unreachable` |
| `VBoxManage showvminfo "UbuntuServer-GestionGimnasios" --details \| grep -i localhost` | Mac / host | Inspecciona opción NAT | `LocalhostReachable: 0` |
| `sudo poweroff` | VM / sudo | Apaga antes de editar virtualización | VM apagada |
| `VBoxManage modifyvm "UbuntuServer-GestionGimnasios" --nat-localhostreachable1 on` | Mac / host | Modifica NAT, permite acceso a loopback del Mac | `LocalhostReachable: 1` |
| Repetir prueba TCP | VM / usuario | Verifica la corrección | `Puerto SQL accesible` |

No se modificó SQL Server ni se creó una nueva base de datos durante este diagnóstico. **La prueba TCP no demuestra por sí sola autenticación**, por lo que se validó más tarde el ingreso real de un usuario y lectura de datos desde el panel web.

### Fase H: proxy, variables de entorno y Compose

| Comando | Equipo / privilegios | Efecto | Evidencia |
|---|---|---|---|
| `mkdir -p ~/gestion-gimnasios/nginx` | VM / usuario | Prepara carpeta del proxy | Creada |
| `cat > ~/gestion-gimnasios/nginx/default.conf <<'EOF' ... EOF` | VM / usuario | Escribe reglas Nginx; archivo de despliegue | Contenido revisado con `cat` |
| `cat > ~/gestion-gimnasios/compose.yaml <<'EOF' ... EOF` | VM / usuario | Define servicios web/nginx, variables y red | Archivo revisado con `cat` |
| `install -m 600 /dev/null ~/gestion-gimnasios/.env` | VM / usuario | Crea archivo privado vacío | `-rw-------` |
| `nano ~/gestion-gimnasios/.env` | VM / usuario | Edita secretos localmente; **nunca revelar** | Variables SQL y JWT presentes |
| `python3` con `secrets.token_urlsafe(48)` | VM / usuario | Genera JWT y actualiza `.env` sin imprimir su valor | `Clave JWT generada y guardada correctamente` |
| `ls -l ~/gestion-gimnasios/.env` | VM / lectura | Verifica propietario/permisos | `600`, `melanie` |
| Script Python de validación (solo informa configurado/revisar) | VM / usuario | Comprueba nombres, valor no temporal y permisos | SQL y JWT configurados, `0o600` |
| `sudo docker compose config --quiet` | VM / sudo | Valida YAML e interpolación sin mostrar secretos | Sin errores |
| `sudo docker compose up -d` | VM / sudo | Descarga Nginx y arranca servicios | 2 contenedores iniciados |
| `sudo docker compose ps` | VM / sudo | Lee estado y puertos | Ambos `Up`, 80 publicado, 8080 interno |
| `curl -I http://127.0.0.1` | VM / usuario | Verifica raíz HTTP | 302 hacia login |
| `curl -s -o /dev/null -w 'HTTP %{http_code}\n' http://127.0.0.1/Cuenta/Login` | VM / usuario | Verifica login | HTTP 200 |

**Parámetros de Compose relevantes:** `ASPNETCORE_ENVIRONMENT=Production`, `BaseDatos__MigrarAlIniciar=false`, `BaseDatos__CargarDatosDePrueba=false`, política `unless-stopped` en web y nginx. `depends_on` controla orden de inicio, **no garantiza que la base de datos externa esté lista**. La base no se incluye entre los servicios Compose.

### Fase I: publicación web, redirección y corrección de Nginx

| Comando / prueba | Equipo / privilegios | Qué hace | Resultado |
|---|---|---|---|
| `lsof -nP -iTCP:8080 -sTCP:LISTEN` | Mac / usuario | Comprueba puerto libre | Ocupado por Java PID 4243 |
| `lsof -nP -iTCP:8081 -sTCP:LISTEN` | Mac / usuario | Comprueba puerto alternativo | Sin procesos; `grep`/`lsof` pueden salir con código 1 |
| Añadir NAT `HTTP-Gimnasios` | VirtualBox | `127.0.0.1:8081 -> 10.0.2.15:80` | Login visible si se usa ruta directa |
| Abrir `http://127.0.0.1:8081/` | Mac / navegador | Comprueba redirección desde raíz | Falló: quitaba `:8081` y aparecía Laravel Herd |
| `sed -i 's/proxy_set_header Host \$host;/proxy_set_header Host \$http_host;/' ~/gestion-gimnasios/nginx/default.conf` | VM / usuario | Modifica encabezado Host en archivo de servidor | Archivo host actualizado |
| `grep 'proxy_set_header Host' ~/gestion-gimnasios/nginx/default.conf` | VM / lectura | Verifica archivo host | `$http_host` |
| `sudo docker compose exec nginx nginx -t` | VM / sudo Docker | Valida sintaxis de Nginx | `test is successful` |
| `sudo docker compose exec nginx nginx -s reload` | VM / sudo Docker | Solicita recarga de config | Señal aceptada, pero fallo persistente |
| `sudo docker compose exec nginx nginx -T 2>&1 \| grep -E 'proxy_pass\|proxy_set_header Host\|proxy_redirect'` | VM / sudo Docker | Ve config **real** cargada | Reveló `$host` anterior |
| `sudo docker compose exec nginx cat /etc/nginx/conf.d/default.conf` | VM / sudo Docker | Lee archivo dentro del contenedor | Confirmó archivo anterior |
| `sudo docker compose up -d --no-deps --force-recreate nginx` | VM / sudo Docker | Recrea solo Nginx y el montaje de config | Éxito; web no recreada |
| `sudo docker compose exec nginx grep 'proxy_set_header Host' /etc/nginx/conf.d/default.conf` | VM / sudo Docker | Verifica nuevo archivo dentro del contenedor | `$http_host` |
| `curl -sSI -H 'Host: 127.0.0.1:8081' http://127.0.0.1/` | VM / usuario | Simula Host del navegador y valida redirección | 302 `Location` con `:8081` |
| Chrome en `http://127.0.0.1:8081/` | Mac / navegador | Evidencia funcional final | Login visible y puerto conservado |

**Causa técnica:** `$host` en Nginx no conservaba el puerto público `8081` de la cabecera HTTP; `$http_host` conserva el valor recibido, incluido el puerto. El archivo configurado del host no coincidía con el archivo montado dentro del contenedor hasta que se recreó Nginx. La evidencia final verificó ambos niveles.

### Fase J: operación funcional y recuperación al reiniciar

| Comando / prueba | Equipo / privilegios | Finalidad | Resultado |
|---|---|---|---|
| Login con cuenta de prueba desde Chrome | Mac / navegador | Valida autenticación real | Acceso exitoso al panel Resumen |
| Vista Resumen | Mac / navegador | Valida consulta real a BD | Mostró 7 usuarios activos, 2 socios con suscripción y sucursal Reyes Equipetrol al momento de prueba |
| Cerrar sesión | Mac / navegador | Valida flujo de salida | Retorno correcto a login conservando 8081 |
| `sudo docker inspect --format '{{.Name}}: {{.HostConfig.RestartPolicy.Name}}' ...` | VM / sudo Docker | Consulta políticas de reinicio | Ambos `unless-stopped` |
| `sudo reboot` | VM / sudo | Reinicia Ubuntu (conexión SSH se corta) | Sistema reinició y aceptó SSH |
| `ssh -i ~/.ssh/gym_server_ed25519 -p 2222 melanie@127.0.0.1` | Mac / usuario | Reconexión a Ubuntu | Exitosa |
| `cd ~/gestion-gimnasios && sudo docker compose ps` | VM / sudo Docker | Confirma inicio automático sin `up` manual | Ambos `Up 2 minutes` |
| `curl -sSI -H 'Host: 127.0.0.1:8081' http://127.0.0.1/` | VM / usuario | Valida respuesta tras reinicio | HTTP 302 con `Location` correcto |

**Diferenciar:** *reboot + restart policy* demuestra recuperación automática de contenedores, **no** prueba restauración de respaldo. La restauración todavía debe ejecutarse y documentarse.

### Fase K: Git y entrega reproducible

| Comando | Equipo / privilegios | Efecto | Resultado |
|---|---|---|---|
| `mkdir -p nginx` | Mac / usuario | Prepara carpeta en repo | Creada |
| Dos comandos `scp -i ... -P 2222 ...` | Mac / usuario | Recuperan `compose.yaml` y `nginx/default.conf` de Ubuntu, no `.env` | 100% transferido |
| Crear `.env.example` con placeholders | Mac / usuario | Documenta variables sin exponer secretos | Archivo público |
| `git check-ignore -v .env .env.example` | Mac / lectura | Comprueba exclusiones | `.env` ignorado; excepción `.env.example` |
| `git status --short && git diff --check` | Mac / lectura | Comprueba estado y espacios en archivos rastreados | Cambios esperados |
| `git add .dockerignore .env.example compose.yaml nginx/default.conf web/GestionGimnasios.Web/Dockerfile web/GestionGimnasios.Web/GestionGimnasios.Web.csproj` | Mac / usuario | Prepara 6 archivos | Preparados |
| `git diff --cached --check && git diff --cached --stat && git diff --cached --name-only` | Mac / lectura | Revisa errores y lista exacta de commit | 6 archivos, 106 inserciones, sin errores |
| `git diff --cached --name-only \| grep -E ...` | Mac / lectura | Busca archivos privados entre staged | Ninguna coincidencia (salida 1 normal) |
| `git diff --cached -- .env.example compose.yaml nginx/default.conf ...` | Mac / lectura | Inspección manual de cambios | Sin secretos visibles en archivos revisados |
| `git commit -m "Configurar despliegue Docker y Nginx en Ubuntu Server"` | Mac / usuario | Registra infraestructura en rama feature | Commit `0af48fb` |

**Pendiente de cierre:** agregar este informe y README a la misma rama, hacer un commit documental, `git push -u origin feature/infraestructura-ubuntu-docker`, abrir PR hacia `develop` y solicitar revisión. No realizar push directo a `main`/`develop`.

## 4. Evidencias gráficas sugeridas para la entrega

Las siguientes evidencias se obtuvieron durante la sesión. Conservar las capturas originales e insertarlas en la entrega final con pies de figura. **No añadir capturas que expongan claves, contraseñas, `.env`, cookies ni tokens.**

| Figura | Evidencia | Estado de comprobación |
|---|---|---|
| 01 | VirtualBox con Ubuntu Server ARM64 y `gym-server login:` | Captura compartida |
| 02 | Snapshot `Ubuntu-Base-SSH-Seguro` | Captura compartida |
| 03 | Login SSH desde terminal del Mac | Terminal compartida |
| 04 | UFW `Status: active`, 22/tcp y 80/tcp | Terminal compartida |
| 05 | Docker `Hello from Docker!` y Compose v5.7.1 | Terminal compartida |
| 06 | BuildKit `15/15 FINISHED`, imagen `gestion-gimnasios-web:1.0` | Terminal compartida |
| 07 | `docker compose ps` con ambos contenedores `Up` | Terminal compartida |
| 08 | Diagnóstico NAT `LocalhostReachable: 0` / `1` | Terminal compartida |
| 09 | Login desde `127.0.0.1:8081` en Chrome | Captura compartida |
| 10 | Panel Resumen con usuario autenticado y datos | Captura compartida |
| 11 | Redirección problemática a Laravel Herd, antes de corrección | Captura compartida |
| 12 | Redirección reparada, `Location` conserva `:8081` | Terminal y captura compartidas |
| 13 | Ambos contenedores `Up` después de `sudo reboot` | Terminal compartida |
| 14 | Backup y restauración real | **PENDIENTE** |
| 15 | Usuario Linux con mínimos permisos | **PENDIENTE** |
| 16 | Cliente MAUI conectado a la API | **PENDIENTE** |
| 17 | Revisión de logs `docker compose logs` | **PENDIENTE** |

**Nota:** una captura solo demuestra lo visible. Para las evidencias de terminal, repetir el comando de lectura si se necesita una imagen con fecha o comando claramente identificables. No simular ni fabricar capturas.

## 5. Matriz de verificación del enunciado

| Criterio de evaluación | Estado | Evidencia / observación |
|---|---|---|
| Identificar recursos y justificar su elección | Documentado | Sección 1.2; pendiente evaluación de carga |
| Ubuntu Server sin GUI | **Cumple** | Instalación ARM64 CLI |
| Hostname e IP identificables | **Cumple** | `gym-server`, `10.0.2.15` |
| SSH desde cliente | **Cumple** | Mac OpenSSH, clave ED25519 |
| UFW solo con puertos necesarios | **Cumple en laboratorio** | TCP 22 y 80; NAT localhost restringido |
| Nginx activo y configurado | **Cumple** | Contenedor, proxy, HTTP 200/302 |
| Sistema y API responden | **Parcial** | Web y rutas API alojadas; no hay prueba individual documentada de endpoint API |
| Sistema conectado a PostgreSQL | **No cumple literalmente** | Funciona con Azure SQL Edge/SQL Server; consultar docente |
| BD no expuesta públicamente | **Parcial / condicionado** | Puerto 1433 publicado en Mac como `0.0.0.0`; comprobar firewall y alcance real antes de afirmar aislamiento |
| Contenedores con política de reinicio | **Cumple** | `unless-stopped`; reinicio efectivo probado |
| Backup creado y restauración probada | **Pendiente** | Snapshot creado, sin restaurar; requiere prueba real |
| Incidente resuelto y documentado | **Cumple** | NAT `LocalhostReachable`, redirección Nginx, rsync |
| Otro estudiante reprodujo README | **Pendiente** | Documento elaborado, falta prueba con otra persona |
| Acceso web desde otro dispositivo/host | **Cumple** | Desde macOS anfitrión a VM |
| Acceso móvil al servidor | **Pendiente** | MAUI utiliza URL editable, sin prueba contra Ubuntu |
| Usuario mínimo privilegio | **Pendiente** | Solo usuario admin y proceso APP_UID en contenedor |
| SSH y PuTTY | **Parcial** | SSH probado; PuTTY no ejecutado en Mac |
| Documentación y comandos | **Borrador preparado** | Revisar y validar por estudiante antes de entregar |

## 6. Operación, resolución de fallos y recuperación

### 6.1 Comprobaciones rápidas (Ubuntu)

```bash
sudo systemctl is-active docker
sudo systemctl is-active ssh.socket
sudo ufw status verbose
cd ~/gestion-gimnasios
sudo docker compose ps
sudo docker compose logs --tail=100 web
sudo docker compose logs --tail=100 nginx
curl -s -o /dev/null -w '%{http_code}\n' http://127.0.0.1/Cuenta/Login
curl -sSI -H 'Host: 127.0.0.1:8081' http://127.0.0.1/
```

**Interpretación:** HTTP `200` en login = contenido servido; HTTP `302` con `Location` que incluye `:8081` = redirección correcta. `502` sugiere fallo de conexión Nginx a web; `500` requiere logs de .NET y revisión de BD; timeout requiere inspección de contenedores, red, firewall y NAT. Los comandos de inspección de logs pueden mostrar información sensible.

### 6.2 Si falla la conexión SQL

1. Mac: verificar Docker Desktop y contenedor `sqlserver2022` con `docker ps --filter 'name=sqlserver2022'`.
2. Mac: probar `nc -vz -G 5 127.0.0.1 1433`.
3. Ubuntu: comprobar `ip route`, `ping -c 4 10.0.2.2` y conexión TCP `timeout 5 bash -c 'echo > /dev/tcp/10.0.2.2/1433'`.
4. En el Mac, verificar `VBoxManage showvminfo "UbuntuServer-GestionGimnasios" --details | grep -i localhost`; `LocalhostReachable: 1` es requerido por esta arquitectura local.
5. Si TCP funciona pero el login falla, revisar logs **sin revelar secretos**, disponibilidad de base `GestionGimnasios` y permisos SQL. No habilitar migraciones automáticamente durante el diagnóstico.

### 6.3 Si cambió Nginx pero sigue usando el archivo anterior

```bash
cd ~/gestion-gimnasios
sudo docker compose exec nginx nginx -t
sudo docker compose exec nginx cat /etc/nginx/conf.d/default.conf
sudo docker compose up -d --no-deps --force-recreate nginx
sudo docker compose exec nginx grep 'proxy_set_header Host' /etc/nginx/conf.d/default.conf
```

La recreación afecta únicamente el servicio `nginx` y puede interrumpir HTTP brevemente.

### 6.4 Backup y restauración (procedimiento recomendado, **NO ejecutado todavía**)

**Advertencia:** los siguientes pasos son un plan de prueba, **no evidencia de que ya se haya realizado**. Elegir primero el alcance del backup: VM completa, configuración Docker o base de datos SQL. Son diferentes.

**Opción A, recuperar la VM:** apagar la VM con `sudo poweroff`; crear snapshot nuevo **después del despliegue** (no sustituye copia externa); simular una modificación reversible en un archivo de prueba; restaurar snapshot; reconectar por SSH y repetir `docker compose ps` y prueba HTTP. **La restauración de un snapshot anterior a Docker eliminaría del estado restaurado la instalación hecha después:** usar un snapshot actual para la prueba de recuperación.

**Opción B, respaldo real de SQL Server:** crear una copia de la base `GestionGimnasios` mediante herramientas SQL compatibles con Azure SQL Edge, almacenarla fuera del contenedor, restaurarla **en una base o instancia de prueba separada** y comprobar tablas/registros. Verificar compatibilidad de herramienta, rutas y permisos antes de hacerlo. No reemplazar la base de datos de desarrollo sin copia y autorización.

**Opción C, configuración:** crear archivo de respaldo de `compose.yaml`, `nginx/default.conf`, Dockerfile, README, y `.env` solo a un almacén seguro cifrado; reproducir desde repositorio y restaurar configuración sin subir secretos.

La demostración académica debe incluir **comando/acción de backup, archivo o punto generado, una alteración controlada, restauración y prueba posterior**, además de la fecha.

## 7. Uso de IA: consultas, aceptación, correcciones y validación

De acuerdo con las reglas de la tarea, se informa el uso de IA como apoyo a investigación, explicación y diagnóstico. Las decisiones operativas correspondieron al estudiante, quien ejecutó comandos, revisó salidas, detectó fallos y verificó resultados.

| Consulta a IA | Recomendación aceptada | Ajustes o correcciones | Verificación del estudiante |
|---|---|---|---|
| Arquitectura ARM64 en VirtualBox | Ubuntu Server ARM64, NAT, SSH y Docker | No intentar ejecutar imagen x86_64 SQL Server estándar en VM ARM64 | OS y conectividad funcional |
| Seguridad SSH | Clave ED25519, deshabilitar root/password | Se conservó terminal inicial hasta probar segunda sesión | Acceso por clave y rechazo sin clave |
| Firewall | Habilitar 22 antes de UFW; permitir 80 | No abrir todos los puertos | `ufw status verbose`, SSH nuevo |
| Docker | Repositorio oficial, Engine y Compose | Se verificó ARM64 y separó de Docker Desktop Mac | `hello-world`, Compose, build exitoso |
| Build .NET | Dockerfile multietapa | Se corrigió paquete vulnerable Pkcs a 9.0.21 | Build Release sin advertencias |
| Transferencia | rsync con exclusiones | **Se corrigió** primer `rsync` que perdía `web/` usando `-R` | Dry-run correcto, transferencia y `ls` |
| Conexión BD | Pruebas de red antes de credenciales | **Se corrigió** `LocalhostReachable: 0` | `LocalhostReachable: 1`, TCP accesible, login funcional |
| Proxy inverso | Nginx → ASP.NET Core | **Se corrigió** `$host` → `$http_host`; además el archivo del contenedor estaba desactualizado | `nginx -T`, recreación Nginx, HTTP 302 con puerto |
| Resiliencia | `unless-stopped` | No confundir reinicio con backup | `sudo reboot`, `docker compose ps`, HTTP válido |
| Documentación | README + informe técnico | Se registraron explícitamente requisitos no cumplidos | Pendiente revisión final y prueba por compañero |

### 7.1 Explicación de privilegios y archivos afectados

- `sudo apt ...` altera paquetes de sistema; requiere administrador.
- `sudo tee /etc/ssh/...`, `sudo ufw ...`, `VBoxManage modifyvm ...` afectan seguridad, red y disponibilidad; requieren privilegio sobre servidor/host virtualizador y validación previa.
- `sudo docker ...` controla daemon Docker, contenedores, imágenes y redes; permite acciones equivalentes a administrador en Ubuntu.
- `cat > archivo`, `nano`, `sed -i`, `install -m 600` modifican únicamente las rutas indicadas con los permisos del usuario actual; pueden sobrescribir archivos y deben revisarse antes de ejecutar.
- `git add/commit` modifica el índice/historial del repositorio local; `push` modifica repositorio remoto y debe hacerse solo hacia rama feature.
- `ssh/scp/rsync` transfiere información o crea sesión cifrada; se debe usar clave SSH privada protegida y evitar transferir `.env`.
- `curl`, `grep`, `ip route`, `ls`, `git status` en estos ejemplos son principalmente lectura; `curl` puede activar acciones si se usa método o endpoint inadecuado, por eso se emplearon GET/HEAD a rutas de consulta.

## 8. Cierre y pendientes de la entrega

**Logro principal demostrado:** servidor Ubuntu Server ARM64 sin escritorio, administrado por SSH con clave y UFW; Docker Compose con Nginx y ASP.NET Core 10; web operativa desde macOS; autenticación real contra Azure SQL Edge externo; navegación corregida; contenedores que se recuperan después de reiniciar el SO. También se documentaron incidentes verificables.

**Antes de marcar «tarea completa»:** demostrar acceso desde MAUI a una API del servidor; crear usuario Linux restringido; obtener logs; crear y **restaurar** un backup; comprobar el README con otra persona; acordar con el docente diferencias PostgreSQL/SQL Server y PuTTY/OpenSSH; entregar capturas finales. No se debe afirmar que estas pruebas se realizaron mientras estén pendientes.

**Apagado seguro al finalizar:** en Ubuntu, `sudo poweroff`; esperar estado `Powered Off` en VirtualBox, cerrar VirtualBox y opcionalmente Docker Desktop si la base de datos no se requiere. En el siguiente inicio, abrir Docker Desktop (base de datos) y VirtualBox; el arranque automático de los contenedores de la VM fue comprobado tras `sudo reboot`.

---

*Documento generado a partir de los comandos, salidas y capturas suministradas durante la sesión. Las pruebas no ejecutadas se identifican expresamente; el estudiante debe revisarlo y validar su reproducibilidad antes de entregar.*
