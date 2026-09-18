# GitHub Environments

Los Environments deben crearse desde **Settings → Environments**. El repositorio no intenta crearlos ni modificar su protección automáticamente. Existen tres destinos de deployment. El sign-off funcional se registra como revisión requerida del PR release y no como un cuarto Environment.

| Environment | Secret de demostración | Inicio | Protección |
|---|---|---|---|
| `dev` | `DEMO_DEPLOY_TOKEN` | Automático desde `develop` o `hotfix/*` | Sin aprobación |
| `qa` | `DEMO_DEPLOY_TOKEN` | Promoción del candidato desplegado en DEV | Required reviewers de release |
| `prod` | `DEMO_DEPLOY_TOKEN` | Job posterior al sign-off; espera aprobación | Required reviewers obligatorio |

Use valores ficticios distintos para el secret en cada Environment de deployment. El workflow solo comprueba que exista; nunca lo imprime. `APP_ENVIRONMENT` se deriva del input que selecciona el Environment; `APP_VERSION` se deriva del run de CI, y commit y branch también se obtienen o validan contra ese run.

## Configuración manual

1. Crear `dev`, `qa` y `prod` respetando exactamente minúsculas.
2. En los tres, agregar el secret `DEMO_DEPLOY_TOKEN` con un valor ficticio diferente.
3. En `qa`, habilitar **Required reviewers** para controlar la instalación del candidato. Para la PoC no aplicar una regla de ramas: los eventos `pull_request` usan una referencia temporal `refs/pull/<n>/merge`; el workflow valida explícitamente que el head sea `release/*` o `hotfix/*` y obtiene sus acciones desde la rama base protegida.
4. En la protección de `main`, exigir deployment exitoso en `qa`, descartar aprobaciones obsoletas cuando aparezcan commits y requerir la revisión funcional de QA mediante CODEOWNERS, reglas organizacionales o un check corporativo.
5. En `prod`, habilitar **Required reviewers** y seleccionar al equipo autorizador de producción. Limitar el deployment a `main`; el workflow se dispara por el merge de un PR `release/*` o `hotfix/*` y conserva el SHA original del candidato.
6. Opcionalmente configurar wait timer según la política organizacional.

La aprobación ocurre antes de ejecutar el job asociado a `prod`; por eso sus secrets solo están disponibles después de autorizarlo. GitHub conserva el historial de deployments, actor, commit, estado y aprobación.

Después del deployment QA y los smoke tests, el workflow termina. El PR `release/* → main` permanece abierto mientras QA realiza pruebas funcionales durante varios días. QA registra su aceptación mediante una revisión requerida sobre el HEAD actual. La rama release es inmutable: un cambio descarta la aprobación y queda bloqueado por no tener un candidato previamente construido en DEV. Después del merge, `prod` solicita una autorización independiente.

> La disponibilidad depende del plan y de la visibilidad. GitHub ofrece Environments y sus reglas de protección en repositorios públicos para los planes actuales. En GitHub Free, Pro y Team, reglas como required reviewers y wait timers solo están disponibles para repositorios públicos. Los repositorios privados o internos requieren un plan compatible, y algunas protecciones siguen reservadas a repositorios públicos. Por eso esta demo se publica con datos completamente ficticios; la aprobación no debe simularse con un script.

## Configuración frente a secrets

El nombre del ambiente no se configura dos veces: el input `environment` enlaza el job con GitHub Environments y alimenta `APP_ENVIRONMENT`. Esto evita que el job apunte a `dev` mientras la aplicación declare otro valor. Los secrets son credenciales sensibles, GitHub los enmascara y deben rotarse; `${{ secrets.DEMO_DEPLOY_TOKEN }}` se resuelve según el Environment del job y nunca se incluye en el artefacto.
