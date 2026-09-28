# GitHub Environments

Los Environments deben crearse desde **Settings → Environments**. Existen tres destinos de deployment y un Environment lógico de aprobación. `qa-signoff` no representa infraestructura: hace visible la aceptación funcional como un gate del pipeline.

| Environment | Secret de demostración | Inicio | Protección |
|---|---|---|---|
| `dev` | `DEMO_DEPLOY_TOKEN` | Automático desde `develop` | Sin aprobación |
| `qa` | `DEMO_DEPLOY_TOKEN` | Promoción del candidato validado en DEV | Required reviewers para autorizar la instalación |
| `qa-signoff` | No aplica | Después de `QA acceptance tests` | Required reviewers de QA funcional |
| `prod` | `DEMO_DEPLOY_TOKEN` | Job posterior al sign-off; espera aprobación | Required reviewers obligatorio |

Use valores ficticios distintos para el secret en cada Environment de deployment. El workflow solo comprueba que exista; nunca lo imprime. `APP_ENVIRONMENT` se deriva del input que selecciona el Environment; `APP_VERSION` se deriva del run de CI, y commit y branch también se obtienen o validan contra ese run.

## Configuración manual

1. Crear `dev`, `qa`, `qa-signoff` y `prod` respetando exactamente minúsculas.
2. En `dev`, `qa` y `prod`, agregar el secret `DEMO_DEPLOY_TOKEN` con un valor ficticio diferente. `qa-signoff` no debe contener secrets.
3. En `qa`, habilitar **Required reviewers** para controlar la instalación del candidato. Para la PoC no aplicar una regla de ramas: los eventos `pull_request` usan una referencia temporal `refs/pull/<n>/merge`; el workflow valida explícitamente que el head sea `release/*` y obtiene sus acciones desde la rama base protegida.
4. En `qa-signoff`, habilitar **Required reviewers** con los responsables de QA funcional y activar **Prevent self-review**. El job solo se habilita después de `QA acceptance tests`.
5. En la protección de `main`, exigir los checks del PR, `QA acceptance tests` y `QA sign-off`; descartar aprobaciones obsoletas cuando aparezcan commits.
6. En `prod`, habilitar **Required reviewers** y seleccionar al equipo autorizador de producción. Limitar el deployment a `main`; el workflow se dispara por el merge de un PR `release/*` y conserva el SHA original del candidato.
7. Opcionalmente configurar wait timer según la política organizacional.

La aprobación ocurre antes de ejecutar el job asociado a `prod`; por eso sus secrets solo están disponibles después de autorizarlo. GitHub conserva el historial de deployments, actor, commit, estado y aprobación.

Después del deployment y las pruebas de aceptación, `QA sign-off` queda visible en estado **Waiting**. El PR `release/* → main` permanece abierto hasta que un reviewer de `qa-signoff` acepta exactamente ese HEAD y digest. La rama release es inmutable: un cambio cancela la ejecución anterior y queda bloqueado por no tener un candidato previamente construido en DEV. Después del merge, `prod` solicita una autorización independiente.

> La disponibilidad depende del plan y de la visibilidad. GitHub ofrece Environments y sus reglas de protección en repositorios públicos para los planes actuales. En GitHub Free, Pro y Team, reglas como required reviewers y wait timers solo están disponibles para repositorios públicos. Los repositorios privados o internos requieren un plan compatible, y algunas protecciones siguen reservadas a repositorios públicos. Por eso esta demo se publica con datos completamente ficticios; la aprobación no debe simularse con un script.

## Configuración frente a secrets

El nombre del ambiente no se configura dos veces: el input `environment` enlaza el job con GitHub Environments y alimenta `APP_ENVIRONMENT`. Esto evita que el job apunte a `dev` mientras la aplicación declare otro valor. Los secrets son credenciales sensibles, GitHub los enmascara y deben rotarse; `${{ secrets.DEMO_DEPLOY_TOKEN }}` se resuelve según el Environment del job y nunca se incluye en el artefacto.
