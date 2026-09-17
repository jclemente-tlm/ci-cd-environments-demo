# GitHub Environments

Los Environments deben crearse desde **Settings → Environments**. El repositorio no intenta crearlos ni modificar su protección automáticamente. Existen tres destinos de deployment y un Environment adicional de gobernanza para el sign-off funcional.

| Environment | Variable `APP_ENVIRONMENT` | Secret de demostración | Inicio | Protección |
|---|---|---|---|---|
| `dev` | `dev` | `DEMO_DEPLOY_TOKEN` | Automático desde `develop` | Sin aprobación |
| `qa` | `qa` | `DEMO_DEPLOY_TOKEN` | Automático desde `main` | Opcional |
| `qa-approval` | No aplica | No requerido | Manual después de pruebas funcionales | Required reviewers de QA |
| `prod` | `prod` | `DEMO_DEPLOY_TOKEN` | Manual, artefacto CI de `main` | Required reviewers obligatorio |

Use valores ficticios distintos para el secret en cada Environment de deployment. El workflow solo comprueba que exista; nunca lo imprime. `APP_VERSION` se deriva del run de CI; commit y branch también se obtienen o validan contra ese run.

## Configuración manual

1. Crear `dev`, `qa`, `qa-approval` y `prod` respetando exactamente minúsculas. `qa-approval` es una puerta de gobernanza, no infraestructura adicional.
2. En `dev`, `qa` y `prod`, agregar la variable `APP_ENVIRONMENT` con el valor correspondiente.
3. En `dev`, `qa` y `prod`, agregar el secret `DEMO_DEPLOY_TOKEN` con un valor ficticio diferente. `qa-approval` no necesita configuración de aplicación ni secrets.
4. En `qa-approval`, habilitar **Required reviewers**, seleccionar al equipo QA, activar prevent self-review y restringir la rama a `main`.
5. En `prod`, habilitar **Required reviewers** y seleccionar al equipo autorizador de producción.
6. En `prod`, limitar deployment branches a `main`; puede aplicarse también a QA.
7. Opcionalmente configurar wait timer según la política organizacional.

La aprobación ocurre antes de ejecutar el job asociado a `prod`; por eso sus secrets solo están disponibles después de autorizarlo. GitHub conserva el historial de deployments, actor, commit, estado y aprobación.

El Environment `qa-approval` aplica la misma mecánica después de que QA termina sus pruebas manuales. La ejecución genera `qa-signoff-<SHA>-<CI_RUN_ID>` con evidencia JSON; el historial del Environment conserva quién autorizó el job. El artifact registra el solicitante y la referencia de pruebas, pero no intenta atribuirse el nombre del reviewer. Un sign-off exitoso inicia automáticamente `Promote PROD`, cuyo job de deployment espera la aprobación independiente de `prod`.

> La disponibilidad depende del plan y de la visibilidad. GitHub ofrece Environments y sus reglas de protección en repositorios públicos para los planes actuales. En GitHub Free, Pro y Team, reglas como required reviewers y wait timers solo están disponibles para repositorios públicos. Los repositorios privados o internos requieren un plan compatible, y algunas protecciones siguen reservadas a repositorios públicos. Por eso esta demo se publica con datos completamente ficticios; la aprobación no debe simularse con un script.

## Variables frente a secrets

Las variables son configuración no sensible y pueden mostrarse en resúmenes. Los secrets son credenciales sensibles, GitHub los enmascara y deben rotarse. Ninguno debe estar hardcodeado en YAML; `${{ vars.APP_ENVIRONMENT }}` y `${{ secrets.DEMO_DEPLOY_TOKEN }}` se resuelven según el Environment del job.
