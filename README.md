# JardinNoemi — tienda online

E-commerce para **JardinNoemi**, un vivero familiar (plantas de interior, suculentas, terrarios, macetas y regalos con vida).
Construido con **Blazor Web App (.NET 10 LTS)**, ASP.NET Core Identity y Entity Framework Core.

## Ejecutar en local

Requisitos: [.NET SDK 10](https://dotnet.microsoft.com/download).

```bash
# 1. (opcional) crear un administrador: las credenciales nunca van en el código
dotnet user-secrets set "Seed:AdminEmail" "tu-correo@ejemplo.cl"
dotnet user-secrets set "Seed:AdminPassword" "UnaClaveSegura123"

# 2. ejecutar
dotnet run --launch-profile https
```

Abre https://localhost:7087 (o http://localhost:5126 con el perfil `http`).
En *Development* la base SQLite (`App_Data/jardinnoemi.db`) se crea, migra y se llena con el catálogo de ejemplo automáticamente.
Para empezar de cero basta con borrar `App_Data/`.

**Pagos de prueba** (pasarela simulada, no hay cargo real):

| Tarjeta | Resultado |
|---|---|
| `4242 4242 4242 4242` | Aprobada |
| `4000 0000 0000 0002` | Rechazada |
| `4000 0000 0000 0119` | Error de pasarela |

Cualquier vencimiento futuro (MM/AA) y CVC de 3 dígitos.

## Arquitectura

- **Blazor Web App con Interactive Server + prerenderizado.** Todo el HTML llega renderizado desde el servidor (SEO y primera
  carga rápida) y luego se vuelve interactivo por SignalR. Los servicios acceden a la base de datos directamente, sin una
  API intermedia. Las páginas de ingreso, registro y cambio de contraseña usan SSR estático (`[ExcludeFromInteractiveRouting]`)
  porque necesitan `HttpContext` para emitir la cookie.
- **Un solo proyecto con capas por carpeta** (Models → Data → Services → Components). Se puede dividir en proyectos
  Core/Infrastructure/Web cuando el equipo crezca.
- **Sin capa Repository**: `DbContext` ya es unidad de trabajo + repositorio. Los servicios usan `IDbContextFactory`
  (un contexto por operación, lo recomendado en Blazor Server).
- **Estado prerender → interactivo** con `[PersistentState]` (.NET 10) para no consultar la base dos veces.

```
Components/
  Layout/      MainLayout, AdminLayout, SiteHeader, MobileNavigation (Popover API), SiteFooter
  Pages/       Home, Shop, ProductDetails, CartPage, CheckoutPage, OrderConfirmation, About, Contact, Policy
    Account/   Login, Register, Profile, Orders, ChangePassword, AccessDenied
    Admin/     Dashboard, Products, ProductEdit, Categories, Orders, OrderDetail, Customers, Inventory
  Catalog/     ProductCard, ProductGrid, CategoryCard, SearchBar, FilterPanel, PriceFilter, ProductGallery
  Cart/        CartDrawer, CartLineItem, CartSummary, FreeShippingProgress
  Checkout/    CheckoutOrderSummary
  Shared/      AppButton, Icon, Modal, Drawer, ToastHost, EmptyState, Pagination, Breadcrumbs, SeoHead, …
  Admin/       StatCard, StatusPill, AdminPageHeader
  Account/     Auth state provider, endpoint de logout, mensajes de Identity en español
Models/        Entidades del dominio
DTOs/          Modelos de lectura y formularios con validación
Data/          DbContext, configuraciones EF, migraciones, seeder y Seed/catalog.json
Services/      Catálogo, carrito, pedidos, envío, pagos (Payments/), admin (Admin/), toasts
Endpoints/     sitemap.xml y robots.txt
wwwroot/css/   tokens → base → components → layout → pages → admin
tools/         generate_art.py (ilustraciones SVG de muestra)
```

## Configuración

| Sección | Qué controla |
|---|---|
| `ConnectionStrings:DefaultConnection` | Base de datos (SQLite por defecto) |
| `Store` | Nombre, contacto, redes, cultura/moneda (`es-CL`/`CLP`), URL base para SEO, datos de transferencia |
| `Shipping` | Tarifa general, tarifas por región, umbral de despacho gratis, días estimados |
| `Database` | `ApplyMigrationsOnStartup` y `SeedSampleData` (activados solo en Development) |
| `Storage` | Carpeta de imágenes subidas desde el panel (fuera de `wwwroot`) y tamaño máximo |
| `Seed:AdminEmail` / `Seed:AdminPassword` | Administrador inicial; solo por user-secrets o variables de entorno (`Seed__AdminPassword`) |

**Reemplazar los datos de ejemplo:** edita `Data/Seed/catalog.json` (categorías, productos, precios, stock, fichas de cuidado
y, opcionalmente, `images`), borra `App_Data/` y vuelve a ejecutar. O gestiona todo desde `/admin` subiendo fotos reales.
Las ilustraciones SVG se regeneran con `python tools/generate_art.py`.

**Supuestos a revisar:** la tienda opera en Chile (regiones, CLP, textos legales); testimonios, cifras de la portada,
historia en "Nosotros", datos bancarios y contacto son de ejemplo.

## Producción

- Migraciones: genera un script o bundle (`dotnet ef migrations bundle`) y aplícalo en el despliegue; deja
  `Database:ApplyMigrationsOnStartup=false`.
- Para SQL Server o PostgreSQL: cambia `UseSqlite` por el proveedor correspondiente en `Program.cs` y regenera las migraciones.
- Blazor Server necesita *sticky sessions* (o Azure SignalR Service) si hay varias instancias.
- Persistir las claves de Data Protection (protegen cookies y el id del carrito) en almacenamiento compartido.
- Pasarela real: implementa `IPaymentGateway` (Stripe, Mercado Pago, Webpay, PayPal) con tokenización en el navegador y
  registra la implementación en `Program.cs` en lugar de `MockPaymentGateway`; confirma pagos con webhooks.

  <img width="1087" height="862" alt="image" src="https://github.com/user-attachments/assets/914efc1e-731d-4ad2-a4ac-0218a521025f" />

