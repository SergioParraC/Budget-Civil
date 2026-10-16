---
agent: speckit.specify
---
Objetivo:
Generar la especificacion del feature inicial de gestion de usuarios y panel de administracion de usuarios/companias, enfocada en que y por que, sin entrar en detalles de implementacion tecnica.

Nombre del feature sugerido:
Sistema de usuarios, autenticacion y administracion de usuarios/companias

Fuentes de verdad obligatorias por prioridad:
1) .specify/memory/constitution.md
2) .github/spec/SPEC.md
3) .github/spec/clarify.md
4) .github/spec/tasks.md
5) .github/docs/02-requerimientos.md
6) .github/docs/03-arquitectura-general.md
7) .github/docs/05-modelo-datos.md
8) .github/docs/06-flujos-casos-de-uso.md
9) .github/docs/09-operacion-copilot-spec-kit.md

Alcance funcional IN-SCOPE de este feature:
1. Login de usuarios con autenticacion JWT mediante username o email.
2. Alta de primer usuario administrador (bootstrap administrado y trazable).
3. Panel de administracion de usuarios:
   - crear usuario
   - editar usuario
   - desactivar usuario (sin borrado fisico)
   - asignar y cambiar rol
   - asignar compania al usuario
4. Panel de administracion de companias:
   - crear compania
   - editar compania
   - desactivar compania (sin borrado fisico)
5. Definir matriz de permisos por rol para el alcance de usuarios/companias:
   - Administrador
   - Gerente de proyectos
   - Presupuestador
   - Auditor
6. Definir regla de autoservicio de cuenta:
   - todos los roles pueden administrar su propio perfil/cuenta y cambio de contrasena dentro de limites de seguridad.
7. Definir restricciones de multitenancy y privacidad por CompanyId.
8. Definir filtros de busqueda en panel de usuarios:
   - email
   - username
   - nombre y apellidos
   - numero de documento

Reglas de negocio obligatorias a capturar:
1. Borrado fisico prohibido para usuarios y companias; solo desactivacion.
2. Un usuario no puede administrarse fuera de los permisos de su rol.
3. Gerente de proyectos no puede administrar otras companias.
4. Gerente de proyectos no puede administrar proyectos de otra compania.
5. No se puede asignar a un proyecto un usuario activo en otra compania.
6. Administrador puede ver todos los proyectos e insumos, pero no intervenirlos en este feature.
7. Restriccion de CompanyId obligatoria en endpoints privados.
8. Un usuario puede pertenecer solo a una compania al tiempo.
9. Un usuario solo puede cambiar de compania si actualmente no pertenece a ninguna compania.
10. Si una cuenta esta bloqueada/deshabilitada, el sistema debe mostrar el mensaje: "Este no se encuentra habilitado, solicite al administrador su habilitacion".

Reglas obligatorias de bootstrap del primer Administrador:
1. El sistema debe detectar si existe al menos un usuario con rol Administrador.
2. Si no existe Administrador, se habilita el flujo de creacion del primer usuario Administrador.
3. Cuando el primer Administrador ya fue creado, el flujo bootstrap debe quedar deshabilitado e idempotente.
4. Reejecutar bootstrap no debe crear un nuevo Administrador.
5. Todo bootstrap debe registrar auditoria con: actor, accion, objetivo, fecha/hora y resultado.
6. Los registros de auditoria de bootstrap solo pueden ser visualizados por rol Administrador.

Reglas obligatorias de credenciales y recuperacion:
1. Contrasena almacenada con algoritmo Argon2id (hash, nunca texto plano).
2. Politica minima de contrasena: minimo 8 caracteres, al menos una mayuscula, un numero y un caracter especial.
3. Recuperacion de contrasena en esta fase: solo mediante solicitud al Administrador.
4. El Administrador habilita cambio de contrasena para el usuario despues de intento(s) de inicio de sesion fallido(s).
5. El diseno debe quedar adaptable para recuperacion por correo electronico en fases futuras.

Campos minimos de usuario a especificar (a nivel funcional):
- primer_nombre (obligatorio), segundo_nombre (opcional)
- primer_apellido (obligatorio), segundo_apellido (opcional)
- tipo_documento (tabla de tipos de documentos)
- numero_documento
- fecha_nacimiento
- residencia: municipio, departamento, pais (Tablas respectivas)
- email
- username
- password (almacenada con hash Argon2id, nunca en texto plano)
- rol_asignado
- tarjeta_profesional
- fecha de expedicion de tarjeta profesional
- Archivo de tarjeta profesional
- fecha_creacion
- fecha_edicion
- fecha_ultimo_login
- estado_cuenta (boolean)
- company_id

Entidades funcionales sugeridas (sin definir aun el modelo fisico):
- UserAccount
- UserProfile
- Role
- Company
- UserCompanyMembership
- UserProjectMembership (solo para reglas de asignacion, no para modulo de proyectos completo)
- AuthSession o LoginAudit

OUT-OF-SCOPE obligatorio en este feature:
1. Construccion completa de modulo de proyectos.
2. Construccion completa de modulo APU/EDS/insumos.
3. Calculo de costos APU, reglas de cascada de cotizacion y cierre de presupuesto.
4. Cualquier funcionalidad fuera de usuarios, autenticacion y administracion de usuarios/companias.

Requisitos de calidad para el spec:
1. Historias de usuario priorizadas e independientes (P1/P2/P3).
2. Criterios de aceptacion medibles por historia.
3. Matriz de trazabilidad FR/UC/endpoint/prueba/tarea.
4. Marcar como NEEDS CLARIFICATION toda ambiguedad.
5. Respetar constitucion: para este feature usar pruebas de caja negra en backend/frontend; pruebas unitarias solo aplican cuando haya logica de calculo APU (no esperada en este feature).

Ambiguedades que MUST quedar como NEEDS CLARIFICATION si no hay definicion explicita:
1. Unicidad de email y username: global del sistema o por compania.
2. Cantidad de intentos fallidos para activar estado bloqueado o flujo de recuperacion asistida por Administrador.
3. Alcance del auditor sobre multiples proyectos/companias.

Salida esperada:
- spec.md completo, trazable y listo para speckit.clarify.
- Secciones explicitas: In-Scope, Out-of-Scope, Matriz de permisos por rol, Reglas de multitenancy, Datos funcionales de usuario, NEEDS CLARIFICATION.
