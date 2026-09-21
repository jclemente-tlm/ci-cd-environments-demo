# Guía de demo

## Preparación

1. Crear los Environments y protecciones descritos en [github-environments.md](github-environments.md).
2. Crear `develop` desde `main` y publicar ambas ramas.
3. Crear rulesets para ambas ramas.
4. En **Actions**, confirmar que los workflows están habilitados.

Antes de ejecutar los escenarios, presentar la estrategia como una transición controlada para un equipo que todavía está consolidando PR, protección de ramas y automatización. El modelo anterior `dev → qa → main` representaba ambientes mediante ramas; la propuesta separa las ramas (`develop`, `main`) de los GitHub Environments (`dev`, `qa`, `prod`) y promueve una única identidad inmutable.

## Escenario 1 — Feature y DEV

```bash
git switch develop
git pull
git switch -c feature/demo-change
# realizar y confirmar un cambio pequeño
git push -u origin feature/demo-change
```

Primero, mostrar que el push ejecuta `Build and unit tests`, `Code quality`, `Security checks` y `Delivery checks`, pero no genera un candidato ni despliega en ningún ambiente. Esto proporciona feedback inmediato antes del PR.

Abrir PR `feature/demo-change → develop`, mostrar nuevamente las validaciones junto con `Validate branch route`, y hacer merge. En Actions, abrir la ejecución **CI/CD Pipeline** del push integrado a `develop` y mostrar las validaciones, la creación del candidato y `Deploy DEV`. El Summary debe indicar Environment, SHA, versión, digest y run de origen. La página de `dev` conserva el deployment.

## Escenario 2 — Promoción del mismo candidato a QA

Tomar el SHA exacto mostrado en DEV y crear la rama que congela el candidato:

```bash
git fetch origin
git switch --detach <SHA_CANDIDATO_MOSTRADO_EN_EL_RUN>
git switch -c release/v0.1.0-demo
git push -u origin release/v0.1.0-demo
```

Abrir el PR `release/v0.1.0-demo → main` con el título `release: v0.1.0-demo`. Incluir versión, SHA, digest y run de CI. El PR debe permanecer abierto durante toda la validación funcional.

Mostrar la ejecución de **CI/CD Pipeline** iniciada por el PR. `Deploy QA` espera la aprobación del Environment `qa`. Después de aprobar, el mismo job localiza el artefacto creado previamente para el HEAD, verifica su identidad y lo despliega sin reconstruirlo.

Abrir el job de QA y mostrar los smoke tests de `/health`, `/environment` y `/version`, además del artefacto `qa-smoke-evidence-<SHA>-<CI_RUN_ID>`. Explicar que una falla genera diagnósticos, pero no evidencia técnica exitosa.

Abrir el Summary de `Deploy QA` y comparar su digest con DEV: debe ser idéntico. El mismo job ejecuta los smoke tests y registra que el candidato quedó listo para validación funcional. El PR permanece abierto mientras QA prueba durante varios días; no queda ningún job esperando. Los pushes posteriores a `develop` actualizan DEV, pero nunca QA.

## Escenario 3 — QA sign-off y merge en `main`

Cuando QA termina, registra una revisión **Approve** sobre el PR. La protección de `main` debe exigir esta revisión funcional, el deployment QA y los checks exitosos para el HEAD actual.

Explicar que `release/*` es inmutable. Si alguien agrega un commit, GitHub descarta la aprobación y **Resolve candidate** falla porque ese SHA no tiene un artefacto validado desde DEV. La corrección debe entrar a `develop`, generar otro candidato y abrir un release nuevo. Sin cambios adicionales, la aprobación de QA habilita el merge.

Fusionar `release/v0.1.0-demo → main` sin construir otro artefacto y eliminar la rama temporal.

Explicar que el PR fija el código correspondiente al digest aunque `develop` haya continuado avanzando. La implementación empresarial deberá comprobar automáticamente que el árbol del PR corresponde al `source_sha` asociado al digest promovido.

## Escenario 4 — Producción

1. Fusionar el PR `release/* → main` después de la aprobación funcional.
2. Mostrar que el evento de merge inicia una ejecución de **CI/CD Pipeline** para PROD.
3. Abrir `Resolve merged candidate` y comprobar que utiliza el SHA original del PR, no el merge commit.
4. Mostrar `Deploy PROD` esperando aprobación del Environment `prod`.
5. Un reviewer distinto aprueba mediante **Review deployments → Approve and deploy**.
6. Abrir el Summary y comprobar que versión, SHA, digest y run coinciden con DEV y QA.

La descarga por nombre `candidate-<SHA>`, la validación del manifiesto y la igualdad del digest demuestran que PROD recibe el mismo binario validado, no una recompilación. En una implementación Docker, el equivalente es desplegar `repository@sha256:<digest>`.

## Escenario alternativo — QA fallido

1. Provocar temporalmente una expectativa incorrecta en uno de los smoke tests o utilizar un candidato defectuoso controlado.
2. Mostrar que el job QA falla y publica `qa-diagnostics-*`, pero no `qa-smoke-evidence-*`.
3. Mostrar que el deployment QA requerido no queda exitoso y, por tanto, el PR no puede fusionarse ni iniciar PROD.
4. Si es un defecto de código, corregirlo mediante una rama `fix/*` y PR hacia `develop`.
5. Mostrar que se crea un candidato nuevo, se despliega primero en DEV y repite todo el ciclo.

## Mensajes clave

- Integración continua y construcción del candidato ocurren en `develop`; `main` registra el código liberado sin reconstruirlo.
- DEV es rápido y automático; QA prueba el candidato; PROD resuelve automáticamente su identidad y requiere aprobación humana.
- El artefacto es inmutable y cada deployment queda asociado a run, commit, digest y actor.
- El PR `release/* → main` se fusiona después del sign-off QA y antes de aprobar PROD.
- La misma definición sirve para los tres ambientes, mientras variables, secrets y protección permanecen aislados.
- Un hotfix tiene vía corta a `main`, pero siempre regresa a `develop`.
