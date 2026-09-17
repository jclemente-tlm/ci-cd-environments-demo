# Flujo CI/CD

Este documento describe la automatización de la PoC. La matriz empresarial de controles, publicación de imágenes y evolución de herramientas se encuentra en [Flujo de calidad y artefactos](quality-and-artifact-flow.md).

## CI

CI se ejecuta en pushes de las ramas del modelo y en PR hacia `develop` o `main`. Realiza restore, build con warnings como errores, tests y publicación de resultados. Solo los pushes a las ramas permanentes `develop` y `main` publican `application-<commit SHA>` con retención de 30 días; las ramas temporales, los PR y las ejecuciones manuales no publican artefactos desplegables.

En el flujo objetivo, PR, `develop` y `main` ejecutan también Semgrep para SAST, SonarQube para análisis de calidad, SCA, secret scanning, container scanning e IaC scanning. Los merges en ramas permanentes repiten los controles sobre el commit integrado; solo entonces publican un artefacto. La PoC todavía no implementa esas herramientas y no debe interpretarse que ya estén operativas. La política de SonarQube bloquea el pipeline cuando su Quality Gate no es exitoso.

Los Markdown no disparan CI en push porque no cambian la aplicación; en PR sí se conserva el check requerido. Por ello, un merge compuesto exclusivamente por documentación no crea artefacto ni deployment, aunque actualice `develop` o `main`. `GITHUB_TOKEN` usa solo `contents: read` en CI.

## CD y promoción

```text
CI(develop, exitoso)     ──download artifact──> Environment dev
CI(main, exitoso)        ──download artifact──> Environment qa
                                                   └──smoke tests──> evidencia QA por SHA
QA Sign-off(run + SHA + referencia)
                         ──validar smoke──> Environment qa-signoff ──aprobación──> sign-off
sign-off exitoso
                         ──resolver candidato automáticamente──> Environment prod ──aprobación──> deploy
```

El CD no ejecuta `dotnet build` ni `dotnet publish`. Descarga el artefacto creado por el run indicado y comprueba que contenga la DLL. Después de desplegar a QA, inicia esa aplicación y valida `/health`, `/environment` y `/version`. Solo si las respuestas coinciden con el ambiente, versión, SHA y rama esperados publica `qa-smoke-evidence-<SHA>-<CI_RUN_ID>`.

Cuando terminan las pruebas funcionales, QA inicia `QA Sign-off` con run de CI, SHA y referencia al plan o ticket. El workflow verifica primero CI y smoke evidence; solo un candidato técnicamente válido solicita la aprobación de `qa-signoff` y publica `qa-signoff-<SHA>-<CI_RUN_ID>`.

DEV cancela un deployment en curso cuando aparece otro más reciente, porque interesa reflejar con rapidez el estado actual de `develop`. QA y PROD no cancelan deployments iniciados: sus ejecuciones conservan la evidencia y las decisiones de promoción asociadas a cada candidato.

Un sign-off exitoso dispara `Promote PROD`. Este descarga la evidencia JSON de esa ejecución, recupera run de CI, SHA y versión, y vuelve a comprobar que CI fue exitoso sobre `main`. Después solicita la aprobación de `prod`. La versión `0.1.0-<CI run ID>` es idéntica en QA, sign-off y PROD; el operador no copia ni puede sobrescribir identificadores técnicos.

## Política de Pull Requests

`PR Policy` convierte las rutas documentadas en un check ejecutable. Permite `feature/*` y `fix/*` hacia `develop`, `develop` hacia `main`, `fix/qa-*` y `hotfix/*` hacia `main`, y `main` hacia `develop` para resincronización. Las ramas de corrección QA y hotfix deben contener el estado actual de `main` y no pueden incluir merges de otra línea de desarrollo. El ruleset debe exigir este check junto con CI.

## Fallas en QA

```text
QA fallido
├── configuración/infraestructura ──> corregir ambiente y reintentar mismo artefacto
└── defecto de código ──> fix/qa-* desde main ──> nuevo CI, artefacto y QA
```

Los fallos conservan logs y respuestas parciales como `qa-diagnostics-*`. El artefacto rechazado nunca se elimina ni se modifica; simplemente no obtiene evidencia de promoción. Toda corrección de código realizada desde `main` debe sincronizarse posteriormente hacia `develop`.

Cada job de deployment genera un resumen con aplicación, Environment, branch, SHA, versión, timestamp UTC y run de origen. El job de sign-off genera su propio resumen de aprobación funcional. El despliegue es una simulación deliberada; una implementación futura sustituiría únicamente ese paso por el adaptador de plataforma, conservando las puertas y metadatos.

## Seguridad

- Actions oficiales versionadas a una versión mayor estable.
- Permisos mínimos: lectura de contenido y, solo en CD, artefactos/actions.
- Secrets definidos por Environment y nunca incluidos en el artefacto o logs.
- Concurrencia de CI cancela ejecuciones obsoletas de una misma referencia.
- Ninguna credencial cloud ni permisos `write` son necesarios.

La reducción de configuración duplicada proviene de `deploy.yml`: los tres ambientes consumen el mismo contrato, pero GitHub inyecta variables, secrets y controles propios. Esto ofrece separación integración/promoción, trazabilidad, historial y gobernanza sin infraestructura real.
