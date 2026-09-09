# Budget Civil 🏗️💰

Bienvenido al repositorio del backend de Budget Civil.

## 👋 Presentación
Budget Civil Backend es la API del proyecto Budget Civil, orientado a la gestión y control de presupuestos para obras civiles.

### 👥 Creadores
- Juan Pablo Ramírez Carmona
- Sergio Steven Parra Cuesta
- David Eduardo Alvarado Martínez

## 🧭 ¿De qué trata el proyecto?
El proyecto busca centralizar la construcción, validación y seguimiento de presupuestos de obra, incluyendo estructuras WBS/APU, reglas de aprobación y trazabilidad para decisiones técnicas y de negocio.

## 🧩 Estrategia de desarrollo (SDD)
Seguimos Spec-Driven Development (SDD): primero se especifica, luego se planifica, se desglosan tareas y finalmente se implementa y converge contra lo definido.

## 🛠️ Tecnologías utilizadas
- ASP.NET Core Web API (.NET 10)
- Entity Framework Core (8.x)
- SQL Server 2022
- Angular 20.x
- Nginx
- Docker Compose v2

## 📚 Estructura actual de la documentación

```text
.github/
	AI-INSTRUCTIONS.md
	copilot-instructions.md
	docs/
		00-idea-problema.md
		01-viabilidad-basica.md
		02-requerimientos.md
		03-arquitectura-general.md
		04-stack-tecnologico.md
		05-modelo-datos.md
		06-flujos-casos-de-uso.md
		09-operacion-copilot-spec-kit.md

docs/
	architecture/
		components.puml
		deployment.puml
		domain-model.dbdiagram
```

## ▶️ Instrucciones para ejecutar
Por ahora, la ejecución se realizará únicamente desde el archivo de solución principal .sln.

1. Abre la solución principal .sln en un IDE Microsoft Visual Studo 2026.
2. Configura los proyectos de inicio dentro de la solución.
3. Ejecuta la solución desde el IDE (Run/F5).

**NOTA:** En el estado actual del proyecto no se tiene un desarrollo iniciado, solamente se encuentra la documentación inicial del proyecto, creada con apoyo de IA.

## 📖 Bibliografía

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

