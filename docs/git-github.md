# Flujo de trabajo con Git y GitHub

## Repositorio

- Remoto: **`https://github.com/jonasmz/restoManager.git`** (remoto `origin`).
- Rama troncal: **`main`**. Es la **única** rama integradora.
- `main` está (o debe estar) **protegida** en GitHub: sin push directo, PR
  obligatorio. Configuración: *Settings → Branches → Branch protection rules* para
  `main` (require pull request, require conversation resolution). Lo hace el
  propietario del repo.

## Reglas duras

Para agentes y personas, sin excepción:

1. **Se desarrolla por features, cada feature en su propia rama.**
2. **La integración a `main` es SOLO por Pull Request.** Nunca se mergea `main`
   localmente.
3. **Prohibido**:
   - `git checkout main && git merge <rama>` (o `git rebase` sobre `main` local).
   - `git push origin main` (push directo a la troncal).
   - `git push --force` / `--force-with-lease` sobre ramas ya compartidas.
   - Acumular varias features en una sola rama/PR.
4. `main` local solo se actualiza con **`git switch main && git pull --ff-only`**
   (el repo tiene `pull.ff = only` configurado).
5. El **merge del PR lo hace una persona** tras revisar. El agente **no** mergea PRs.

## Ciclo de una feature

```bash
# 1. Partir de main actualizada
git switch main
git pull --ff-only

# 2. Crear la rama de la feature
git switch -c feat/<area>-<resumen-corto>
#   prefijos válidos: feat/ fix/ refactor/ docs/ chore/ test/ perf/ build/ ci/
#   ejemplos: feat/inventory-movimientos  fix/orders-canal-barra  docs/backend-testing

# 3. Commits pequeños y atómicos (Conventional Commits, mensaje en español)
git add -p
git commit   # ver formato de mensaje abajo

# 4. Publicar la rama
git push -u origin feat/<area>-<resumen-corto>

# 5. Abrir el Pull Request hacia main (ver más abajo)

# 6. Tras el merge del PR por una persona:
git switch main
git pull --ff-only
git branch -d feat/<area>-<resumen-corto>
git push origin --delete feat/<area>-<resumen-corto>   # o botón "Delete branch" en GitHub
```

**Una feature = una rama = un PR.**

## Formato de commit

[Conventional Commits](https://www.conventionalcommits.org). Asunto en español, en
imperativo, ≤ 72 caracteres. Cuerpo opcional explicando el *por qué*.

```
feat(inventory): registra movimientos con balance en una transacción

Aplica INV-05: el movimiento y la actualización de branch_inventory se
persisten juntos. Añade InventoryMovementHandler y sus tests.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017Wex9JNuuB1Q4FTDNwrUkj
```

Los dos *trailers* finales (`Co-Authored-By` y `Claude-Session`) son **obligatorios**
en cada commit hecho por un agente.

## Pull Request

- **Base:** `main`. **Compare:** la rama de la feature.
- **Título:** el mismo estilo que el commit principal
  (`feat(inventory): registra movimientos …`).
- **Descripción** (plantilla):

  ```markdown
  ## Qué
  <resumen del cambio>

  ## Contra qué requisito
  <sección o IDs del spec: DOM-01, INV-05, ORD-06, §5.4, …>

  ## Decisiones menores tomadas sin consultar
  - <nombre de campo / texto de UI / estructura interna / …>

  ## Cómo verificar
  <comandos contenedorizados, pantallas a revisar, tests>

  🤖 Generated with [Claude Code](https://claude.com/claude-code)
  ```

- **Estrategia de merge recomendada:** *Squash and merge* (historia lineal en
  `main`, un commit por feature). La decisión final es del propietario del repo.
- No se mergea con checks en rojo ni con conversaciones sin resolver.

## Credenciales (cómo accede el agente a GitHub)

- Acceso por **HTTPS con un Personal Access Token** fine-grained, con permisos
  mínimos sobre `jonasmz/restoManager`: **Contents: RW**, **Pull requests: RW**,
  **Metadata: RO**.
- El token **no se guarda en el repo** ni en ningún fichero en texto plano. Vive en
  el **llavero del sistema** (Secret Service / KWallet), accesible con `secret-tool`:
  - Guardar / rotar:
    `secret-tool store --label='GitHub PAT restoManager' service github.com account restoManager-pat`
    (pide el token por stdin; no lo pases como argumento).
  - Consultar (lo hace el helper, normalmente no a mano):
    `secret-tool lookup service github.com account restoManager-pat`
- Git obtiene el token mediante un *credential helper* propio,
  `~/.local/bin/git-credential-kwallet-restomanager`, configurado **solo para
  `https://github.com`** y a nivel de repo
  (`git config credential."https://github.com".helper …`).
- **El PAT nunca va en un commit, en logs, en `appsettings*.json` ni en `.env`.**
  `.gitignore` bloquea `.env*` y ficheros de clave. Si un token se filtra en un
  commit: revocarlo en GitHub de inmediato, generar otro y reescribir la historia
  afectada antes de que llegue a `main`.
- Si el llavero está bloqueado en la sesión (sin entorno gráfico / D-Bus), fallback
  temporal: `git config --global credential.helper 'cache --timeout=3600'` (solo
  memoria) y reintroducir el token una vez.

## Integración continua (a futuro)

Todavía no hay `.github/workflows/`. Cuando se añada CI (build + test de backend y
frontend en contenedor, lint), el PAT necesitará además **Workflows: RW** para poder
hacer push de cambios bajo `.github/workflows/`.
