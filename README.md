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

La automatización actual es deliberadamente mínima: implementa build, pruebas unitarias, empaquetado, promoción y smoke tests de QA. Los controles empresariales adicionales descritos en la documentación, incluidos cobertura y escaneos de seguridad, representan el estado objetivo y todavía no se ejecutan.

- `ci-cd-pipeline.yml` (`CI and DEV`): valida PR, compila y prueba. Un push integrado a `develop` —o excepcionalmente a `hotfix/*`— crea el candidato y lo despliega en DEV.
- `release-qa.yml` (`Release QA`): ante un PR `release/* → main`, localiza por SHA el artefacto ya construido, despliega el mismo digest en QA, ejecuta smoke tests y termina.
- `release-prod.yml` (`Release PROD`): después del merge del PR, recupera el SHA original y el digest aceptado, espera la aprobación de `prod` y despliega sin reconstruir.
- `.github/actions/deploy/action.yml`: encapsula la descarga, simulación, trazabilidad y smoke tests compartidos sin aparecer como otro workflow en Actions.
- `.github/actions/resolve-candidate/action.yml`: resuelve automáticamente el run, versión y digest del candidato para evitar entradas técnicas manuales.

La promoción no solicita run ID, SHA, versión ni nombre de artefacto. Son decisiones diferentes: el Environment `qa` autoriza instalar un candidato; una revisión requerida del PR registra que QA terminó sus pruebas funcionales y aceptó su HEAD y digest; el merge registra el código aprobado; y `prod` autoriza ejecutar el deployment productivo.

El PR `release/<versión> → main` permanece abierto durante toda la validación funcional. La revisión de `QA sign-off` debe ser obligatoria: solo después de aceptar el HEAD cuyo deployment identifica el digest actual puede fusionarse. La rama release es inmutable; si el código debe cambiar, se rechaza, se genera otro candidato desde `develop` y se abre un release nuevo. Después del merge, `prod` aplica una autorización independiente.

El workflow de QA termina después del deployment y los smoke tests. El PR permanece abierto durante las pruebas funcionales, sin mantener un job de Actions pendiente. Configurar protección para descartar aprobaciones obsoletas cuando cambie el HEAD y exigir tanto el deployment QA exitoso como la revisión funcional antes del merge.

## Configuración de GitHub

Crear manualmente los Environments `dev`, `qa` y `prod`. En los tres definir el secret ficticio `DEMO_DEPLOY_TOKEN`. Configurar required reviewers en `qa` y `prod`; QA funcional se registra como revisión requerida del PR, no mediante un Environment adicional. Ningún valor secreto se registra o se incluye en el artefacto.

Configurar rulesets para impedir pushes directos a `main` y `develop`, exigir PR y los checks `Build and test` y `Validate branch route` exitosos. En `main`, exigir además el deployment QA y la revisión funcional. Los detalles y comandos de demo están en:

- [Estrategia de ramas](docs/branching-strategy.md)
- [Decisiones de arquitectura](docs/architecture-decisions.md)
- [Flujo de calidad y artefactos](docs/quality-and-artifact-flow.md)
- [GitHub Environments](docs/github-environments.md)
- [Flujo CI/CD](docs/ci-cd-flow.md)
- [Guía de demo](docs/demo-guide.md)

## Decisión de diseño

GitHub Environments no se crean desde los workflows porque requiere permisos administrativos y ocultaría una parte importante de la demo. El pipeline no recompila entre ambientes: DEV, QA y PROD reciben el artefacto identificado por el mismo SHA, run y digest. La versión es un metadato de promoción, no una compilación nueva.

CI/DEV, QA y PROD son workflows separados por eventos duraderos: integración en `develop`, PR release abierto o actualizado, y PR release fusionado. Los deployments reutilizan una composite action que recalcula y valida la huella antes de cada despliegue; otra acción resuelve automáticamente el candidato por SHA. No se copian identificadores manualmente.

La rama permanente `qa` del modelo anterior `dev → qa → main` se reemplaza por el GitHub Environment `qa`. Las únicas ramas permanentes son `develop` y `main`; `develop` produce candidatos y `main` registra código liberado sin volver a construirlo. El Environment `prod` registra qué versión está realmente desplegada.
