# Flujo de calidad y artefactos

## Propósito

Este documento define el flujo objetivo empresarial para validación, construcción, publicación y promoción. La regla central es:

> Los Pull Requests validan cambios; las ramas permanentes validan el estado integrado y producen artefactos desplegables.

La PoC implementa actualmente build, pruebas unitarias, empaquetado, almacenamiento del artefacto y smoke tests de QA. SAST, SCA, secret scanning, container scanning e IaC scanning forman parte del flujo objetivo y deben incorporarse con las herramientas aprobadas por la organización.

## Flujo objetivo

El siguiente diagrama representa el estado objetivo, no las capacidades que ya ejecuta esta PoC. La tabla de [implementación incremental](#implementación-incremental) distingue ambos estados.

```text
feature/* o fix/*
        ↓
PR hacia develop
└── CI — validación del cambio
    ├── build de la solución
    ├── pruebas unitarias y cobertura
    ├── SAST de seguridad (Semgrep)
    ├── análisis de calidad (SonarQube)
    ├── SCA/dependency scanning (OWASP Dependency Check)
    ├── secret scanning (Gitleaks)
    ├── lint del Dockerfile (Hadolint)
    ├── análisis de imagen de contenedor (Trivy Image)
    ├── IaC scanning (Checkov), cuando exista IaC
    ├── validación del título del PR/Conventional Commits
    └── publicar reportes y resúmenes
        ↓ merge
develop
├── CI — validación integrada y artefacto DEV
│   ├── build de la solución
│   ├── pruebas unitarias y cobertura
│   ├── SAST de seguridad (Semgrep)
│   ├── análisis de calidad (SonarQube)
│   ├── SCA/dependency scanning (OWASP Dependency Check)
│   ├── secret scanning (Gitleaks)
│   ├── lint del Dockerfile (Hadolint)
│   ├── IaC scanning (Checkov), cuando exista IaC
│   ├── construir imagen/artefacto inmutable
│   ├── análisis de imagen de contenedor (Trivy Image)
│   ├── publicar reportes y resúmenes
│   └── publicar imagen/artefacto de desarrollo
└── CD — DEV
    ├── desplegar automáticamente el artefacto de develop
    └── habilitar validación integrada de funcionalidades
        ↓ PR de promoción; desarrollo puede continuar
PR develop → main
└── CI — validación de promoción
    ├── revisión de promoción
    ├── build de la solución
    ├── pruebas unitarias y cobertura
    ├── SAST de seguridad (Semgrep)
    ├── análisis de calidad (SonarQube)
    ├── SCA/dependency scanning (OWASP Dependency Check)
    ├── secret scanning (Gitleaks)
    ├── lint del Dockerfile (Hadolint)
    ├── análisis de imagen de contenedor (Trivy Image)
    ├── IaC scanning (Checkov), cuando exista IaC
    ├── validación del título del PR/Conventional Commits
    ├── publicar reportes y resúmenes
    └── no publicar ni desplegar desde el PR
        ↓ merge
main
├── CI — candidato oficial
│   ├── build de la solución
│   ├── pruebas unitarias y cobertura
│   ├── SAST de seguridad (Semgrep)
│   ├── análisis de calidad (SonarQube)
│   ├── SCA/dependency scanning (OWASP Dependency Check)
│   ├── secret scanning (Gitleaks)
│   ├── lint del Dockerfile (Hadolint)
│   ├── IaC scanning (Checkov), cuando exista IaC
│   ├── construir candidato oficial inmutable
│   ├── análisis de imagen de contenedor (Trivy Image)
│   ├── publicar reportes y resúmenes
│   ├── resolver versión/release
│   └── publicar por SHA/versión/digest
└── CD — QA
    ├── desplegar automáticamente el candidato
    └── ejecutar smoke tests automáticos
        ↓
QA — validación funcional
├── ejecutar pruebas funcionales manuales
└── QA sign-off para el SHA/digest exacto
    ↓
GATE — PROD
└── aprobación productiva
    ↓
CD — PROD
└── desplegar el mismo digest validado en QA
```

## Matriz de controles

| Control | PR a `develop` | Merge en `develop` | PR `develop → main` | Merge en `main` |
|---|:---:|:---:|:---:|:---:|
| Build | Sí | Sí | Sí | Sí |
| Pruebas unitarias | Sí | Sí | Sí | Sí |
| Cobertura de código | Objetivo | Objetivo | Objetivo | Objetivo |
| SAST de seguridad (Semgrep) | Sí | Sí | Sí | Sí |
| Análisis de calidad (SonarQube) | Sí | Sí | Sí | Sí |
| SCA/dependencias | Sí | Sí | Sí | Sí |
| Secret scanning | Sí | Sí | Sí | Sí |
| Lint del Dockerfile | Sí | Sí | Sí | Sí |
| IaC scanning, cuando exista IaC | Sí | Sí | Sí | Sí |
| Construir artefacto desplegable | No | Sí | No | Sí |
| Análisis de imagen de contenedor (Trivy) | Sí | Sí | Sí | Sí |
| Reportes y resúmenes | Sí | Sí | Sí | Sí |
| Validación semántica del PR | Sí | No aplica | Sí | No aplica |
| Publicar artefacto/imagen | No | Sí, desarrollo | No | Sí, candidato oficial |
| Deployment | No | DEV | No | QA |
| Elegible para PROD | No | No | No | Solo después de QA sign-off |

Los controles se repiten después del merge porque el commit integrado no es necesariamente idéntico al commit validado en el PR y puede incluir interacciones con otros cambios ya fusionados.

## Validación contra un proyecto de referencia

Como insumo de diseño se revisaron workflows de un servicio .NET de referencia. Se omite deliberadamente su nombre para que esta PoC pública no identifique repositorios ni sistemas internos. El catálogo encontrado y su correspondencia con esta propuesta es:

| Workflow/módulo de referencia | Herramienta o propósito | Control documentado |
|---|---|---|
| `_build.yml` | Restore y build .NET | Build |
| `_test.yml` | Tests unitarios y Coverlet/OpenCover | Pruebas y cobertura |
| `_scan-sast.yml` | SonarQube, métricas y Quality Gate | Calidad de código y análisis complementario |
| `_scan-secrets.yml` | Gitleaks | Secret scanning |
| `_scan-iac.yml` | Checkov sobre Terraform | IaC scanning |
| `_scan-dockerfile.yml` | Hadolint | Lint del Dockerfile |
| `_scan-deps.yml` | OWASP Dependency Check | SCA/dependencias |
| `_scan-trivy.yml` | Trivy FS e Image | Análisis de imagen; FS queda como control complementario |
| `commitlint.yml` | Conventional Pull Request title | Gobernanza del PR |
| `_semantic-release.yml` | Versión, tag y changelog | Gestión de release, no análisis de seguridad |
| `_publish.yml` | Build/push o retag en ECR | Publicación y promoción del artefacto |
| `_deploy.yml` | OIDC, Terraform init/apply y ECS | Deployment, no CI |

Pasos operativos como checkout, configuración del SDK, restauración de dependencias, cachés, instalación de scanners y preparación de reportes son necesarios en la implementación, pero se omiten deliberadamente del flujo arquitectónico. No representan gates ni decisiones de promoción y pueden cambiar sin alterar el modelo.

Con esta comparación, todos los análisis implementados actualmente en ese proyecto están representados. Los siguientes controles no aparecen implementados allí y deben tratarse como evolución, no como capacidad existente:

- DAST contra una aplicación desplegada; el propio workflow CD lo menciona como futuro.
- Generación y validación de SBOM.
- Firma de imágenes, provenance y attestations.
- Escaneo de licencias con una política explícita.
- Pruebas de integración, contrato, end-to-end o performance, salvo que estén dentro de proyectos de test no visibles en la orquestación.

Las notificaciones mostradas en el diagrama de referencia tampoco aparecen implementadas como módulo en los workflows revisados. Pueden añadirse como capacidad transversal para fallos, despliegues, rechazos QA y promociones productivas.

Semgrep no estaba presente en el servicio .NET de referencia, pero se incorpora como requisito explícito de esta propuesta para SAST. Se observó su uso en otro proyecto de referencia; para servicios .NET deben definirse reglas apropiadas para C#/.NET y seguridad general, no reutilizar configuraciones específicas de otro lenguaje o framework.

### Ejecución frente a enforcement

El inventario no implica que todos los resultados bloqueen actualmente un merge. En el proyecto revisado se observó:

| Control | Comportamiento observado |
|---|---|
| SonarQube | Bloquea cuando el Quality Gate no devuelve `OK` |
| Semgrep | Debe bloquear según la política de reglas y severidades aprobada; la política concreta está pendiente de estandarización para .NET |
| Gitleaks | Bloquea cuando detecta secretos |
| Checkov | Configurado con `soft_fail: false`; bloquea ante checks fallidos |
| OWASP Dependency Check | Bloquea por severidad crítica; vulnerabilidades altas generan advertencia |
| Trivy Image y FS | Generan reportes, pero `continue-on-error` y `|| true` evitan que bloqueen |
| Cobertura | Se genera y publica; no se identificó un umbral mínimo explícito en el workflow de test |
| Hadolint | Ejecuta el análisis y publica reportes; su política final debe normalizarse explícitamente |
| Validación semántica del PR | Bloquea títulos que no cumplen los tipos permitidos |

Antes de estandarizar la propuesta se debe acordar una política por control: **block**, **warn** o **report only**, además de severidades, excepciones con vencimiento y responsables. Ejecutar un scanner sin definir su efecto no constituye por sí mismo una puerta de calidad.

### Alcance de Trivy

El flujo arquitectónico conserva únicamente **Trivy Image**. Este analiza la imagen final, incluidos paquetes del sistema operativo, dependencias incorporadas, capas y componentes agregados durante el build.

El comando `trivy fs .` del proyecto de referencia analiza el workspace del runner, no el filesystem interno del contenedor. Con la configuración por defecto cubre vulnerabilidades de dependencias y secretos, por lo que se solapa con OWASP Dependency Check y Gitleaks. Se mantiene como defensa en profundidad opcional en la implementación técnica, pero no como una categoría independiente del flujo principal.

## Política de construcción durante Pull Requests

Un PR nunca publica una imagen o artefacto desplegable en el registro empresarial. El container scanning puede construir internamente una imagen efímera, pero esta es un detalle de ejecución del scanner y debe descartarse al finalizar.

```text
PR: construir → escanear → descartar
merge: construir → escanear → publicar → desplegar
```

Esto evita contaminar el registro con candidatos no aprobados y reduce el riesgo de desplegar un artefacto creado desde un contexto de PR.

## Artefactos de `develop`

Un merge en `develop` genera un artefacto real porque DEV debe ejecutar el estado integrado exacto:

```text
application:dev-<sha>
application:develop        # alias móvil opcional
```

- `dev-<sha>` es inmutable y debe utilizarse para trazabilidad.
- `develop` puede ser un alias móvil para conveniencia, pero no constituye identidad suficiente para auditar un deployment.
- Tiene retención más corta y no puede promoverse a PROD.
- Su finalidad es validar integración y preparar el siguiente release.

## Artefactos de `main`

Un merge en `main` genera el candidato oficial:

```text
application:sha-<sha>
application:<version>
digest: sha256:<digest>
```

QA y PROD deben utilizar el mismo digest:

```text
digest(QA) == digest(PROD)
```

Los tags ayudan a localizar una imagen, pero el digest demuestra su identidad. CD no reconstruye, modifica ni reetiqueta contenido durante la promoción.

## Trabajo paralelo

La rama `develop` permite continuar integrando el siguiente conjunto de cambios mientras `main` permanece como candidato bajo validación:

```text
main:    versión 1.2.0 → QA → sign-off → PROD
develop: funcionalidades integradas para 1.3.0 → DEV
```

Esta necesidad de estabilización paralela justifica mantener dos ramas permanentes en lugar de adoptar GitHub Flow puro con una única `main`.

## QA manual y promoción

Los smoke tests demuestran salud técnica, pero no sustituyen las pruebas funcionales. Para promover a PROD se requieren evidencias independientes:

1. CI exitoso para el commit de `main`.
2. Artefacto oficial identificado por SHA y digest.
3. Deployment QA y smoke tests exitosos.
4. Sign-off funcional manual de QA para ese SHA.
5. Aprobación productiva.

Si QA rechaza un candidato, este no obtiene sign-off y no puede llegar a PROD. Un defecto de código se corrige mediante `fix/qa-*` desde `main`, genera un nuevo SHA y repite el ciclo completo. Una falla exclusivamente ambiental permite reintentar el mismo artefacto.

## Implementación incremental

| Capacidad | PoC actual | Evolución empresarial |
|---|---|---|
| Build y unit tests | Implementado; 2 pruebas unitarias | Ampliar la suite y las políticas |
| Medición de cobertura | No implementada | Generar reporte y definir umbral |
| Artefacto .NET inmutable | Implementado con GitHub Artifacts | Registro empresarial |
| Imagen Docker | Build local implementado | Publicación en GHCR/ECR/Artifactory/Nexus |
| Smoke tests QA | Implementado | Ejecutarlos contra infraestructura real |
| Semgrep/SCA/secrets/IaC/container scanning | Documentado | Seleccionar reglas, severidades y quality gates |
| QA funcional manual | Sign-off protegido implementado | Integrar herramienta corporativa de pruebas si aplica |
| Aprobación PROD | Environment documentado | Integrar change management si aplica |
| Digest y attestations | Documentado | Firma, SBOM y provenance |
