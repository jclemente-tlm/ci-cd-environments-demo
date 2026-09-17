# Guía de demo

## Preparación

1. Crear los Environments y protecciones descritos en [github-environments.md](github-environments.md).
2. Crear `develop` desde `main` y publicar ambas ramas.
3. Crear rulesets para ambas ramas.
4. En **Actions**, confirmar que los workflows están habilitados.

Antes de ejecutar los escenarios, presentar la estrategia como una variante simplificada de Gitflow con prácticas de GitHub Flow. Aclarar que el modelo anterior `dev → qa → main` representaba ambientes mediante ramas, mientras que la propuesta separa ramas (`develop`, `main`) de GitHub Environments (`dev`, `qa`, `prod`).

## Escenario 1 — Feature y DEV

```bash
git switch develop
git pull
git switch -c feature/demo-change
# realizar y confirmar un cambio pequeño
git push -u origin feature/demo-change
```

Abrir PR `feature/demo-change → develop`, mostrar los checks `Build and test` y `Validate branch route`, y hacer merge. En Actions, abrir la ejecución **CI/CD Pipeline** del push y mostrar los jobs `Build and test` y `Deploy DEV`; el Summary de este último debe indicar Environment, SHA, rama, versión y timestamp. La página de `dev` conserva el deployment.

## Escenario 2 — Promoción a QA

Abrir PR `develop → main`, aprobar y hacer merge. **CI/CD Pipeline** crea un artefacto nuevo para el SHA de `main`; el job `Deploy QA` lo descarga y lo despliega automáticamente. Mostrar el Summary y el historial del Environment `qa`.

Abrir el job de QA y mostrar los smoke tests de `/health`, `/environment` y `/version`, además del artefacto `qa-smoke-evidence-<SHA>-<CI_RUN_ID>`. Explicar que una falla genera diagnósticos, pero no evidencia técnica exitosa.

Después de los smoke tests, mostrar que el job `Approve QA` permanece esperando en el Environment `qa-signoff`. El equipo ejecuta las pruebas manuales sobre ese candidato y, cuando concluye, selecciona **Review deployments → Approve and deploy**. No introduce identificadores técnicos.

## Escenario 3 — Producción

1. En la misma ejecución de **CI/CD Pipeline**, mostrar que `Deploy PROD` se habilita después de `Approve QA`.
2. Mostrar el job esperando aprobación del Environment `prod`.
3. Un reviewer distinto aprueba mediante **Review deployments → Approve and deploy**.
4. Abrir el Summary y comprobar que versión, SHA y run coinciden con QA.

La descarga por nombre `application-<SHA>` y run ID demuestra que PROD recibe el mismo binario validado, no una recompilación.

## Escenario 3B — QA fallido

1. Provocar temporalmente una expectativa incorrecta en uno de los smoke tests o utilizar un candidato defectuoso controlado.
2. Mostrar que el job QA falla y publica `qa-diagnostics-*`, pero no `qa-smoke-evidence-*`.
3. Mostrar que `Approve QA` y `Deploy PROD` quedan omitidos porque `Deploy QA` no concluyó exitosamente.
4. Si es un defecto de código, crear `fix/qa-demo-failure` desde `main`, corregirlo y abrir PR hacia `main`.
5. Mostrar el nuevo CI, artefacto y QA exitoso.
6. Abrir después PR `main → develop` para sincronizar la corrección.

## Escenario 4 — Hotfix

```bash
git switch main
git pull
git switch -c hotfix/critical-api-error
# corregir, probar, confirmar y publicar
git push -u origin hotfix/critical-api-error
```

Abrir PR a `main`, ejecutar los mismos pasos QA/PROD y después abrir obligatoriamente PR `main → develop`. Mostrar que CI también valida la resincronización. Esta última acción evita divergencia y pérdida futura del hotfix.

## Mensajes clave

- Integración continua ocurre en `develop`; promoción estable ocurre desde `main`.
- DEV es rápido y automático; QA prueba el candidato; PROD resuelve automáticamente su identidad y requiere aprobación humana.
- El artefacto es inmutable y cada deployment queda asociado a run, commit y actor.
- La misma definición sirve para los tres ambientes, mientras variables, secrets y protección permanecen aislados.
- Un hotfix tiene vía corta a `main`, pero siempre regresa a `develop`.
