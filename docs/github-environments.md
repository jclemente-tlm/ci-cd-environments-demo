# GitHub Environments

Los Environments deben crearse desde **Settings → Environments**. El repositorio no intenta crearlos ni modificar su protección automáticamente. Existen tres destinos de deployment y un Environment adicional de gobernanza para el sign-off funcional.

| Environment | Secret de demostración | Inicio | Protección |
|---|---|---|---|
| `dev` | `DEMO_DEPLOY_TOKEN` | Automático desde `develop` | Sin aprobación |
| `qa` | `DEMO_DEPLOY_TOKEN` | Automático desde `main` | Opcional |
| `qa-signoff` | No requerido | Job automático; espera al terminar pruebas funcionales | Required reviewers de QA |
| `prod` | `DEMO_DEPLOY_TOKEN` | Job posterior al sign-off; espera aprobación | Required reviewers obligatorio |

Use valores ficticios distintos para el secret en cada Environment de deployment. El workflow solo comprueba que exista; nunca lo imprime. `APP_ENVIRONMENT` se deriva del input que selecciona el Environment; `APP_VERSION` se deriva del run de CI, y commit y branch también se obtienen o validan contra ese run.

## Configuración manual

1. Crear `dev`, `qa`, `qa-signoff` y `prod` respetando exactamente minúsculas. `qa-signoff` es una puerta de gobernanza, no infraestructura adicional.
2. En `dev`, `qa` y `prod`, agregar el secret `DEMO_DEPLOY_TOKEN` con un valor ficticio diferente. `qa-signoff` no necesita configuración de aplicación ni secrets.
3. En `qa-signoff`, habilitar **Required reviewers**, seleccionar al equipo QA, activar prevent self-review y restringir la rama a `main`.
4. En `prod`, habilitar **Required reviewers** y seleccionar al equipo autorizador de producción.
5. En `prod`, limitar deployment branches a `main`; puede aplicarse también a QA.
6. Opcionalmente configurar wait timer según la política organizacional.

La aprobación ocurre antes de ejecutar el job asociado a `prod`; por eso sus secrets solo están disponibles después de autorizarlo. GitHub conserva el historial de deployments, actor, commit, estado y aprobación.

El Environment `qa-signoff` aplica la misma mecánica después de los smoke tests. El job queda pendiente mientras QA realiza sus pruebas manuales y el historial del Environment conserva quién lo autorizó. Al aprobarlo, el job `Deploy PROD` del mismo pipeline solicita la aprobación independiente de `prod`; ningún operador introduce run ID, SHA o versión.

> La disponibilidad depende del plan y de la visibilidad. GitHub ofrece Environments y sus reglas de protección en repositorios públicos para los planes actuales. En GitHub Free, Pro y Team, reglas como required reviewers y wait timers solo están disponibles para repositorios públicos. Los repositorios privados o internos requieren un plan compatible, y algunas protecciones siguen reservadas a repositorios públicos. Por eso esta demo se publica con datos completamente ficticios; la aprobación no debe simularse con un script.

## Configuración frente a secrets

El nombre del ambiente no se configura dos veces: el input `environment` enlaza el job con GitHub Environments y alimenta `APP_ENVIRONMENT`. Esto evita que el job apunte a `dev` mientras la aplicación declare otro valor. Los secrets son credenciales sensibles, GitHub los enmascara y deben rotarse; `${{ secrets.DEMO_DEPLOY_TOKEN }}` se resuelve según el Environment del job y nunca se incluye en el artefacto.
