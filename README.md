# Budget Civil Backend

Backend del sistema **Budget Civil**, orientado a la gestion de usuarios, companias, proyectos y procesos asociados a presupuestos de obra civil, con enfoque en seguridad por roles, trazabilidad y aislamiento por compania.

---

## 1) Nombre y descripcion del proyecto

**Nombre**: Budget Civil Backend  
**Descripcion**: API REST del dominio de presupuestos de obra civil. Se encarga del login y los permisos, de la administración de usuarios y compañías, y de todo el ciclo de vida del presupuesto de un proyecto. También define los contratos de datos que usan los clientes frontend y los procesos de integración.

---

## 2) Integrantes

- Juan Pablo Ramirez Carmona
- Sergio Steven Parra Cuesta
- David Eduardo Alvarado Martinez

---

## 3) Tecnologias utilizadas

### Backend
- **ASP.NET Core Web API (.NET 10)**: exposicion de endpoints HTTP y pipeline de middleware.
- **C#**: implementacion de reglas de negocio y casos de uso.
- **Entity Framework Core + SQL Server**: persistencia relacional, mapeo ORM y control de esquema.

### Arquitectura y calidad
- **Arquitectura en capas (Api, Application, Domain, Infrastructure)**: separacion de responsabilidades y mantenibilidad.
- **JWT**: autenticacion y autorizacion de endpoints privados.

### Control de versiones
- **Git + GitHub**: control de cambios, historial de commits y colaboracion del equipo.

---

## 4) Arquitectura general

El backend implementa una arquitectura en 4 capas:

- **Api**: controladores, middleware, autenticacion/autorizacion y contratos HTTP.
- **Application**: casos de uso y orquestacion de reglas del negocio.
- **Domain**: entidades, invariantes y reglas centrales del dominio.
- **Infrastructure**: persistencia, repositorios e integraciones tecnicas.

Flujo general:
1. Cliente (frontend/consumidor API) invoca endpoint.
2. Api valida autenticacion/autorizacion.
3. Application ejecuta el caso de uso.
4. Domain aplica reglas del negocio.
5. Infrastructure persiste/consulta en SQL Server.
6. Api retorna respuesta estandarizada.

Documentacion de soporte:
- `specs/` (especificaciones, planes, tareas y contratos)
- `docs/architecture/` (diagramas de arquitectura y modelo de datos)
- `.github/docs/` (requerimientos y lineamientos generales del proyecto)

---

## 5) Estructura del repositorio (resumen)

```text
backend/
  BudgetCivil.Backend.sln
  src/
    BudgetCivil.Api/
    BudgetCivil.Application/
    BudgetCivil.Domain/
    BudgetCivil.Infrastructure/
  tests/
    BudgetCivil.Backend.BlackBoxTests/

docs/
  architecture/

specs/
  001-gestion-usuarios-companias/
  002-gestion-companias-proyectos/
```

---

## 6) Instalacion y ejecucion local

### Prerrequisitos
- .NET SDK 10
- SQL Server 2022 (o instancia compatible)
- Git
- IDE recomendado: Visual Studio

### Pasos
1. Clonar repositorio:
   ```bash
   git clone https://github.com/SergioParraC/Budget-Civil.git
   cd Budget-Civil-Backend
   ```
2. Restaurar paquetes:
   ```bash
   dotnet restore backend/BudgetCivil.Backend.sln
   ```
3. Compilar solucion:
   ```bash
   dotnet build backend/BudgetCivil.Backend.sln
   ```
4. Ejecutar API:
   ```bash
   dotnet run --project backend/src/BudgetCivil.Api/BudgetCivil.Api.csproj
   ```

---

## 7) Variables de entorno y configuracion

La configuracion base se encuentra en:
- `backend/src/BudgetCivil.Api/appsettings.json`
- `backend/src/BudgetCivil.Api/appsettings.Development.json`

### Variables clave esperadas
- `ConnectionStrings__BudgetCivilDatabase`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SigningKey`

### Ejemplo de configuracion local
```json
{
  "ConnectionStrings": {
    "BudgetCivilDatabase": "Server=localhost;Database=BudgetCivilDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Issuer": "BudgetCivil.Api",
    "Audience": "BudgetCivil.Client",
    "SigningKey": "REEMPLAZAR_POR_SECRETO_DE_AL_MENOS_32_CARACTERES"
  }
}
```

---

## 8) Informacion de despliegue

- **Estado actual**: despliegue orientado a entorno local de desarrollo.
- **Objetivo de plataforma**: Linux Ubuntu 24.04 para despliegues del backend.

---

## Bibliografia

- Caro Ortega, J. A., & Guerrero Herrera, C. E. (2022). Estrategias para la optimización del control de costos en proyectos de construcción con base en la aplicación de la metodología del Project Management Institute – PMI [Trabajo de grado, Universidad de Cartagena]. https://repositorio.unicartagena.edu.co/server/api/core/bitstreams/2b64a6d3-5133-4e03-9051-4903b2b7e8f9/content

- Espinal Rojas, A. F. (2020). Software para procesos estandarizados de gestión de proveedores y materiales en empresas del sector de la construcción en Colombia [Trabajo de grado, Universidad Autónoma de Occidente]. https://repositorioslatinoamericanos.uchile.cl/items/ea7a0ec3-9c63-4fdb-8856-38ad4bd5e3f9

- Gómez-Valdés, M., Acevedo-Acevedo, S., Alvarado-Acuña, L., & Iturra-Molina, R. (2023). Impacto de la metodología BIM en la gestión de proyectos de construcción. Revista Tecnología en Marcha. https://doi.org/10.18845/tm.v36i7.6860

- López de Ortigosa Casares, D. Arturo. (2010). Ingeniería de costos en la construcción (Primera). Editorial Trillas.

- Mejía Gutiérrez, W. G., & Peña Romero, M. L. (2024). La transformación digital como herramienta en la modernización de la gestión de proyectos en el  sector de la construcción en la ciudad de Villavicencio - Meta [Trabajo de grado, Corporación Universitaria Minuto de Dios]. https://repository.uniminuto.edu/items/78fa52ee-e3f2-4d3d-a954-34ab3c5cff65

- Ocampo Hurtado, J. G. (2014). La gerencia BIM como sistema de gestión para proyectos de construcción. Revista GTI, 14, 17–29. https://revistas.uis.edu.co/index.php/revistagti/article/view/4868

- Prada Perez, A. M., & Robayo Lequizamo, N. J. (2023). Factores que generan sobre costos en la construcción en el sector público en Colombia [Universidad Santo Tomás]. https://repository.usta.edu.co/handle/11634/53525?show=full

- Schaufelberger, J., & Holm, L. (2021). Construction Cost Estimating (Vol. 1). Routledge / Taylor & Francis Group. https://doi.org/10.1201/9781003023494

- GitHub. (2026). GitHub Copilot [Software]. GitHub. https://github.com/features/copilot

> **Nota:** Se utilizó GitHub Copilot como herramienta de asistencia para la generación de especificaciones a partir de una documentación base con enfoque en desarrollo SDD y resolución de ambigüedades en la documentación inicial.

- Google. (2026). Gemini [Modelo de inteligencia artificial]. Google. https://gemini.google.com/

> **Nota:** Se utilizó Google Gemini como herramienta de razonamiento técnico y modelado de dominio para la estructuración del planteamiento del problema, los requerimientos funcionales y la alineación bibliográfica con estándares de ingeniería de costos. Asimismo, actuó como facilitador en la resolución de ambigüedades de arquitectura (definición de reglas en cascada, contratos DTO sanitizados, algoritmos de recursividad en matrices y estrategias de firma auditada) previas a la fase de codificación.
