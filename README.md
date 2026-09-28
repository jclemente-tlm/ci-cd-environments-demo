# ci-cd-environments-demo

PoC de demostración basada en una API mínima en .NET 10 que muestra cómo construir un candidato una sola vez y promover exactamente la misma identidad por `dev`, `qa` y `prod` usando GitHub Environments. El despliegue es simulado: no crea infraestructura ni almacena credenciales reales. Una huella SHA-256 del paquete representa el digest que tendría una imagen Docker en un registro empresarial.

El repositorio está diseñado para publicarse sin referencias a organizaciones, repositorios, sistemas o credenciales reales. El uso de un repositorio público permite probar required reviewers y otras reglas de protección de GitHub Environments en planes donde esas capacidades no están disponibles para repositorios privados.

La propuesta utiliza una **estrategia de dos ramas con promoción por ambientes**: una variante simplificada de Gitflow que incorpora prácticas de GitHub Flow para ramas temporales y Pull Requests. No es una implementación estricta de ninguno de esos modelos; está adaptada para reducir complejidad sin perder la separación entre integración, código estable y producción protegida.

## Arquitectura

```text
feature/*, fix/* ──PR──> develop ──build único──> candidato por digest
                                              ├──DEV automático
                                              └──release/* ──PR abierto──> main
                                                               ├──QA + pruebas
                                                               ├──QA sign-off
                                                               ├──merge main
                                                               └──aprobación PROD──> PROD
```

La API expone `/`, `/environment`, `/version` y `/health`. Sus metadatos provienen de `APP_ENVIRONMENT`, `APP_VERSION`, `APP_COMMIT_SHA`, `APP_BRANCH` y `APP_DEPLOYED_AT`; solo existen defaults seguros para ejecución local.

## Ejecución local

```bash
dotnet restore
dotnet test
APP_ENVIRONMENT=local APP_VERSION=0.1.0 dotnet run --project src/Cicd.Demo.Api
curl http://localhost:8080/
```

Con Docker:

```bash
docker compose up --build
curl http://localhost:8080/environment
```

## Automatización

La automatización actual es deliberadamente mínima: implementa build y pruebas unitarias reales, y simula los gates de calidad, seguridad y escaneo del artefacto. Solo después de que todos esos gates terminan correctamente genera el paquete desplegable, verifica su digest y permite publicarlo. La promoción y los smoke tests de QA reutilizan ese mismo paquete.

- `ci-cd-pipeline.yml` (`Continuous Integration`) muestra solamente los gates de ramas temporales. `develop-delivery.yml` construye, publica, despliega y valida DEV. `release-to-qa.yml` resuelve el candidato, despliega QA, ejecuta sus pruebas y espera `QA sign-off`. `production-deployment.yml` verifica esa aprobación antes de permitir PROD. Cada ejecución muestra únicamente las etapas que le corresponden.
- `.github/actions/deploy/action.yml`: encapsula la descarga, verificación y simulación de despliegue. `.github/actions/smoke-test/action.yml` ejecuta después la validación compartida de DEV, QA y PROD como jobs visibles e independientes.
- `.github/actions/resolve-candidate/action.yml`: resuelve automáticamente el run, versión y digest del candidato para evitar entradas técnicas manuales.

La promoción no solicita run ID, SHA, versión ni nombre de artefacto. Son decisiones diferentes: el Environment `qa` autoriza instalar un candidato; `QA acceptance tests` valida el deployment; el Environment lógico `qa-signoff` registra que QA aceptó su HEAD y digest; el merge registra el código aprobado; y `prod` autoriza ejecutar el deployment productivo.

El PR `release/<versión> → main` permanece abierto durante toda la validación funcional. El job protegido `QA sign-off` debe ser obligatorio: solo después de aceptar el HEAD cuyo deployment identifica el digest actual puede fusionarse. La rama release es inmutable; si el código debe cambiar, se rechaza, se genera otro candidato desde `develop` y se abre un release nuevo. Después del merge, `prod` aplica una autorización independiente.

Después del deployment y las pruebas de QA, el job `QA sign-off` permanece visible en estado de espera hasta la aceptación funcional. Configurar protección para cancelar evidencia obsoleta cuando cambie el HEAD y exigir `QA acceptance tests` y `QA sign-off` antes del merge.

## Configuración de GitHub

Crear los Environments `dev`, `qa`, `qa-signoff` y `prod`. Definir el secret ficticio `DEMO_DEPLOY_TOKEN` solamente en los tres destinos de deployment. Configurar required reviewers en `qa`, `qa-signoff` y `prod`; `qa-signoff` es un gate lógico sin secrets. Ningún valor secreto se registra o se incluye en el artefacto.

Configurar rulesets para impedir pushes directos a `main` y `develop`, exigir PR y los checks de CI `Validate`, `Build and unit tests`, `Code quality`, `Security checks` y `Delivery checks`. `Scan packaged artifact` se ejecuta después del merge en `develop`, no sobre ramas temporales. En `main`, exigir además `QA acceptance tests` y `QA sign-off`. Los detalles y comandos de demo están en:

- [Estrategia de ramas](docs/branching-strategy.md)
- [Decisiones de arquitectura](docs/architecture-decisions.md)
- [Flujo de calidad y artefactos](docs/quality-and-artifact-flow.md)
- [GitHub Environments](docs/github-environments.md)
- [Flujo CI/CD](docs/ci-cd-flow.md)
- [Guía de demo](docs/demo-guide.md)

## Decisión de diseño

GitHub Environments no se crean desde los workflows porque requiere permisos administrativos y ocultaría una parte importante de la demo. El pipeline no recompila entre ambientes: DEV, QA y PROD reciben el artefacto identificado por el mismo SHA, run y digest. La versión es un metadato de promoción, no una compilación nueva.

CI, DEV, QA y PROD se separan en workflows activados por eventos duraderos: trabajo temporal, integración en `develop`, PR release abierto o actualizado, y merge en `main`. Los deployments reutilizan una composite action que recalcula y valida la huella; otra acción resuelve automáticamente el candidato por SHA. `QA sign-off` publica evidencia inmutable que PROD vuelve a validar contra SHA, digest, versión y run antes de solicitar su aprobación.

La rama permanente `qa` del modelo anterior `dev → qa → main` se reemplaza por el GitHub Environment `qa`. Las únicas ramas permanentes son `develop` y `main`; `develop` produce candidatos y `main` registra código liberado sin volver a construirlo. El Environment `prod` registra qué versión está realmente desplegada.
