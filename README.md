# ci-cd-environments-demo

PoC de demostración basada en una API mínima en .NET 10 que muestra cómo GitHub Environments mejora el proceso de CI/CD, la promoción del mismo artefacto y la protección de despliegues hacia `dev`, `qa` y `prod`. El despliegue es simulado: no crea infraestructura ni almacena credenciales reales.

El repositorio está diseñado para publicarse sin referencias a organizaciones, repositorios, sistemas o credenciales reales. El uso de un repositorio público permite probar required reviewers y otras reglas de protección de GitHub Environments en planes donde esas capacidades no están disponibles para repositorios privados.

La propuesta utiliza una **estrategia de dos ramas con promoción por ambientes**: una variante simplificada de Gitflow que incorpora prácticas de GitHub Flow para ramas temporales y Pull Requests. No es una implementación estricta de ninguno de esos modelos; está adaptada para reducir complejidad sin perder la separación entre integración, código estable y producción protegida.

## Arquitectura

```text
feature/*, fix/* ──PR──> develop ──CI/artefacto──> DEV automático
                              │
                              └──PR──> main ──artefacto oficial──> QA automático
                                                    └──QA sign-off
                                                          └──aprobación PROD──> PROD
hotfix/* ──PR──> main ──> QA ──> PROD; después main ──PR──> develop
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

- `ci.yml`: restaura, compila, prueba y publica resultados. Solo los pushes a `develop` y `main` generan el artefacto desplegable `application-<sha>`.
- `cd.yml`: tras CI exitoso despliega `develop` a DEV y `main` a QA. QA ejecuta smoke tests y registra evidencia técnica por SHA y run de CI. PROD exige CI, smoke tests y sign-off funcional para la misma identidad antes de solicitar aprobación.
- `qa-signoff.yml`: registra la aprobación funcional manual mediante el Environment protegido `qa-approval`.
- `promote-prod.yml`: toma automáticamente la evidencia del sign-off y solicita aprobación en `prod`, sin pedir al operador run ID ni SHA.
- `pr-policy.yml`: valida las combinaciones permitidas de rama origen/destino y el origen de correcciones QA y hotfixes.
- `deploy.yml`: reutiliza el mismo artefacto, enlaza el job al GitHub Environment y escribe trazabilidad en el Job Summary.

La promoción a PROD se inicia automáticamente después de un sign-off QA exitoso, pero el deployment queda esperando aprobación del Environment `prod`. La versión, el SHA y el run de CI se obtienen de evidencia estructurada y no son editables por el operador. La aprobación productiva es una puerta adicional, no un sustituto de QA.

## Configuración de GitHub

Crear manualmente los destinos `dev`, `qa` y `prod`, además de la puerta de gobernanza `qa-approval`. En los tres destinos definir `APP_ENVIRONMENT` y el secret ficticio `DEMO_DEPLOY_TOKEN`. Configurar required reviewers en `qa-approval` y `prod`, con restricción a `main`. Ningún valor secreto se registra o se incluye en el artefacto.

Configurar rulesets para impedir pushes directos a `main` y `develop`, exigir PR y los checks `CI` y `PR Policy` exitosos. Los detalles y comandos de demo están en:

- [Estrategia de ramas](docs/branching-strategy.md)
- [Decisiones de arquitectura](docs/architecture-decisions.md)
- [Flujo de calidad y artefactos](docs/quality-and-artifact-flow.md)
- [GitHub Environments](docs/github-environments.md)
- [Flujo CI/CD](docs/ci-cd-flow.md)
- [Guía de demo](docs/demo-guide.md)

## Decisión de diseño

GitHub Environments no se crean desde los workflows porque requiere permisos administrativos y ocultaría una parte importante de la demo. El pipeline tampoco recompila entre ambientes: QA y PROD reciben el artefacto identificado por el mismo SHA y run de CI. La versión es un metadato de promoción, no una compilación nueva.

La rama permanente `qa` del modelo anterior `dev → qa → main` se reemplaza por el GitHub Environment `qa`. Las únicas ramas permanentes son `develop` y `main`; `main` contiene código promovible, mientras que el Environment `prod` registra qué versión está realmente desplegada.
