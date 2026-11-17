# StockSuper

Sistema web de control de stock y proveedores para cadenas de supermercados.
Trabajo Final Integrador de Programación Web.

- **Sitio en producción:** _pendiente (se completa al desplegar)_
- **Repositorio:** _pendiente_

## Qué resuelve

Cada empresa cliente administra el stock de todas sus sucursales sin que los datos se mezclen
con los de otra empresa. El stock se registra por pallet (con ubicación en el depósito, cantidad
y vencimiento) y cada entrada o salida queda guardada como un movimiento que no se puede editar
ni borrar. Con esos movimientos el sistema calcula reportes por período, alertas de reposición y
de vencimiento, y un asistente de IA redacta informes y pedidos a proveedores.

## Tecnologías

- ASP.NET Core MVC (.NET 10) y Entity Framework Core con migraciones (Code First)
- SQL Server
- ASP.NET Core Identity con roles
- Bootstrap 5, SweetAlert2 y validación con jQuery Unobtrusive
- QuestPDF (reportes en PDF) y MailKit (envío de correos)
- API de IA por HTTP (Groq por defecto; también admite Google Gemini)

## Roles

| Rol | Alcance | Qué puede hacer |
|---|---|---|
| Administrador de plataforma | Toda la plataforma, sin acceso al stock | Empresas, sucursales y cuentas de acceso |
| Dirección de empresa | Su empresa, todas las sucursales | Catálogo, proveedores, consulta de stock, reportes, alertas, pedidos con IA |
| Usuario de sucursal | Una sola sucursal | Sectores de depósito, pallets, movimientos, reportes y alertas de su sucursal |

No existe registro público: las cuentas las crea el administrador, con una contraseña temporal
que se debe cambiar en el primer ingreso.

## Funcionalidades principales

- **Multiempresa:** filtros globales de EF Core aíslan los datos por empresa y por sucursal.
- **Catálogo:** categorías, marcas, proveedores y productos, con baja lógica.
- **Depósito:** sectores, pallets y movimientos de stock con control de concurrencia.
- **Reportes:** stock inicial, entradas, salidas y stock final por período, en pantalla, PDF o por correo.
- **Alertas:** productos bajo el stock mínimo, pallets vencidos y por vencer.
- **IA:** chat asistente flotante que responde sobre el stock y el uso del sistema, informe ejecutivo de las alertas y redacción del pedido de reposición a un proveedor.
- **Auditoría:** cada registro guarda qué cuenta lo creó y lo modificó, y cuándo. Además hay una bitácora de eventos
  (altas, cambios, ingresos, intentos fallidos, cuentas, correos enviados): el administrador ve los eventos de plataforma y
  de acceso, y la Dirección ve los de su empresa. El administrador nunca ve operaciones de stock.
- **Remitos:** cada pallet admite un remito en PDF, guardado en una carpeta privada del servidor y descargable solo por
  cuentas de la misma empresa.
- **Provincias y localidades:** se eligen de un listado oficial (Georef, datos.gob.ar) en lugar de escribirse.
- **Plataforma:** alta de empresas con su casa central, sucursales (se cierran, no se eliminan) y cuentas.

## Modelo de datos

Tablas de negocio (todas con campos de auditoría), además de las de Identity:
`Empresas`, `Sucursales`, `Categorias`, `Marcas`, `Proveedores`, `Productos`,
`SectoresDeposito`, `Pallets` y `MovimientosStock`, más la bitácora `RegistrosAuditoria`.

## Cómo ejecutarlo en desarrollo

Requisitos: SDK de .NET 10 y SQL Server (LocalDB alcanza).

1. Clonar el repositorio y ubicarse en la carpeta del proyecto.
2. Cargar los secretos. **Ninguna clave se guarda en el repositorio:**

   ```bash
   dotnet user-secrets set "SeedUsuarios:AdminPassword" "..."
   dotnet user-secrets set "SeedUsuarios:DirectorPassword" "..."
   dotnet user-secrets set "SeedUsuarios:SucursalPassword" "..."
   dotnet user-secrets set "IA:ApiKey" "..."
   dotnet user-secrets set "Correo:Usuario" "..."
   dotnet user-secrets set "Correo:Password" "..."
   ```

   Las contraseñas deben tener al menos 8 caracteres, con mayúscula, minúscula y número.
   El proveedor de IA se elige en `appsettings.json` (`IA:Proveedor`: `Groq` o `Gemini`).
   El correo y la IA son opcionales: sin esas claves el sistema funciona y avisa que no están configurados.

3. Ejecutar:

   ```bash
   dotnet run
   ```

   Al iniciar se aplican las migraciones y se crean los roles, una empresa de demostración
   y las tres cuentas de prueba.

## Credenciales de prueba

| Rol | Usuario | Contraseña |
|---|---|---|
| Administrador de plataforma | admin@stocksuper.com | _Admin2026x1_ |
| Dirección de empresa | director@demo.com | _Director20261_ |
| Usuario de sucursal | sucursal202@demo.com | _Sucursal2026x_ |

## Seguridad

- Claves y contraseñas fuera del código: User Secrets en desarrollo y configuración del servidor en producción.
- Contraseñas con hash de Identity, bloqueo por intentos fallidos y cambio obligatorio de la contraseña temporal.
- Token antifalsificación en todos los formularios y en las llamadas AJAX.
- La API Key de IA nunca llega al navegador: las consultas a la IA las hace el servidor.
