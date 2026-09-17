# Flujo CI/CD

Este documento describe la automatización de la PoC. La matriz empresarial de controles, publicación de imágenes y evolución de herramientas se encuentra en [Flujo de calidad y artefactos](quality-and-artifact-flow.md).

## CI

CI se ejecuta en pushes de las ramas del modelo y en PR hacia `develop` o `main`. Realiza restore, build con warnings como errores, tests y publicación de resultados. En pushes también publica `application-<commit SHA>` con retención de 30 días; los PR no publican artefactos desplegables.

En el flujo objetivo, PR, `develop` y `main` ejecutan también Semgrep para SAST, SonarQube para análisis de calidad, SCA, secret scanning, container scanning e IaC scanning. Los merges en ramas permanentes repiten los controles sobre el commit integrado; solo entonces publican un artefacto. La PoC todavía no implementa esas herramientas y no debe interpretarse que ya estén operativas. La política de SonarQube bloquea el pipeline cuando su Quality Gate no es exitoso.

Los Markdown no disparan CI en push porque no cambian la aplicación; en PR sí se conserva el check requerido. `GITHUB_TOKEN` usa solo `contents: read` en CI.

## CD y promoción

```text
CI(develop, exitoso)     ──download artifact──> Environment dev
CI(main, exitoso)        ──download artifact──> Environment qa
                                                   └──smoke tests──> evidencia QA por SHA
QA Sign-off(run + SHA + referencia)
                         ──validar smoke──> Environment qa-approval ──aprobación──> sign-off
workflow_dispatch(run + SHA)
                         ──validar CI + smoke + sign-off──> Environment prod ──aprobación──> deploy
```

El CD no ejecuta `dotnet build` ni `dotnet publish`. Descarga el artefacto creado por el run indicado y comprueba que contenga la DLL. Después de desplegar a QA, inicia esa aplicación y valida `/health`, `/environment` y `/version`. Solo si las respuestas coinciden con el ambiente, versión, SHA y rama esperados publica `qa-smoke-evidence-<SHA>-<CI_RUN_ID>`.

Cuando terminan las pruebas funcionales, QA inicia `QA Sign-off` con run de CI, SHA y referencia al plan o ticket. El workflow verifica CI y smoke evidence, espera aprobación de `qa-approval` y publica `qa-signoff-<SHA>-<CI_RUN_ID>`.

Para PROD se exige: workflow `CI` exitoso sobre `main`, SHA coincidente, smoke evidence no expirada y sign-off funcional no expirado. Después se aplican las reglas de aprobación de `prod`. La versión `0.1.0-<CI run ID>` se deriva una sola vez y es idéntica en QA, sign-off y PROD; el operador no puede sobrescribirla.

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
