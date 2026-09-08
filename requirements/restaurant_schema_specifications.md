**RESTAURANT SYSTEM**

Especificación funcional y de dominio  
del esquema de datos

*Uso previsto, invariantes, responsabilidades y flujos operativos*

| **Campo**             | **Valor**                                                                             |
|-----------------------|---------------------------------------------------------------------------------------|
| Documento             | Especificación de uso del esquema de base de datos                                    |
| Versión               | 2.0                                                                                   |
| Fecha                 | 8 de septiembre de 2026                                                               |
| Esquema de referencia | restaurant_schema_v3.sql                                                              |
| Motor objetivo        | PostgreSQL (el índice único parcial de sesiones usa sintaxis PostgreSQL)              |
| Ámbito                | Restaurante multisucursal: operación de mesas, barra, pedidos, clientes, ventas e inventario |

*Cambios respecto de la versión 1.0: se corrigieron 11 hallazgos de una*
*auditoría del esquema (ver sección 13) y se incorporó la funcionalidad*
*de consumo en barra como canal de venta explícito (sección 5.4),*
*deliberadamente no modelado como una mesa (sección 6.4).*

*Este documento define cómo debe interpretarse y utilizarse el esquema.
No sustituye la lógica de dominio de la aplicación; establece el
contrato entre aplicación y persistencia.*

# 1. Objetivo y alcance

El esquema modela la operación integral de un restaurante con una o más
sucursales. Incluye estructura organizativa, mesas y reservas, clientes,
pedidos y pagos, menú y recetas, abastecimiento, inventario,
desperdicios, delivery, descuentos, impuestos, gift cards y reseñas.

La especificación pone especial énfasis en las decisiones de dominio
acordadas para clientes anónimos, ocupación de mesas y trazabilidad de
inventario, porque son las áreas donde la interpretación del modelo
afecta directamente el comportamiento de la aplicación.

## 1.1 Principios de diseño

- La base de datos conserva hechos persistentes e invariantes; la
  aplicación implementa las transiciones y reglas de negocio.

- Los estados que pueden derivarse de hechos más fundamentales no deben
  duplicarse como una segunda fuente de verdad.

- Un consumidor no identificado puede generar consumo y ventas sin
  convertirse en un registro permanente de customers.

- El inventario se gestiona por sucursal y debe disponer tanto de saldo
  actual como de un libro de movimientos auditable.

- Las operaciones que modifican varios registros relacionados deben
  realizarse dentro de una única transacción de base de datos.

## 1.2 Convenciones normativas

En este documento, DEBE indica una regla obligatoria; DEBERÍA indica una
recomendación fuerte; PUEDE indica comportamiento permitido pero
opcional.

# 2. Mapa funcional del esquema

| **Módulo**    | **Entidades principales**                                                     | **Responsabilidad**                                      |
|---------------|-------------------------------------------------------------------------------|----------------------------------------------------------|
| Organización  | restaurants, branches, departments, roles, employees, shifts, employee_leaves | Estructura de la empresa, sucursales y personal.         |
| Salón         | tables, table_sessions, reservations                                          | Capacidad física, operatividad, ocupación y reservas de mesas (no incluye barra, ver 4.5).|
| Venta         | orders, order_items, payments, discounts, order_discounts                     | Cuenta, consumo, cobro, promociones y canal de venta (MESA/BARRA/TAKEAWAY/DELIVERY, ver 5.4). |
| Cliente       | customers, reviews, gift_cards, gift_card_transactions                        | Personas identificadas, fidelización y beneficios.       |
| Menú y cocina | categories, menu_items, recipe_items, kitchen_stations, station_menu_items    | Oferta comercial, recetas y estaciones de preparación.   |
| Inventario    | ingredients, branch_inventory, inventory_movements, waste_logs                | Catálogo de insumos, saldos por sucursal y trazabilidad. |
| Compras       | suppliers, purchase_orders, purchase_order_items                              | Abastecimiento de materias primas.                       |
| Delivery      | delivery_drivers, deliveries                                                  | Asignación y seguimiento de entregas.                    |
| Fiscal        | tax_rates, menu_item_taxes                                                    | Impuestos aplicables a productos del menú.               |

# 3. Clientes y consumidores

## 3.1 Definición semántica de customer

customers representa a un cliente identificado del que el restaurante
conserva datos persistentes (nombre, apellido, email, teléfono y puntos
de fidelidad). No representa obligatoriamente a toda persona que
consume.

**CUS-01 Cliente identificado.** Un registro en customers DEBE
interpretarse como una identidad persistente del negocio, no como un
simple asiento o consumidor temporal.

**CUS-02 Consumidor anónimo.** La aplicación DEBE poder registrar una
venta presencial sin crear un customer ficticio.

## 3.2 Pedidos anónimos

orders.customer_id es nullable. Su significado es:

| **Valor**            | **Interpretación**                                                               |
|----------------------|----------------------------------------------------------------------------------|
| customer_id = \<id\> | Pedido asociado a un cliente identificado existente.                             |
| customer_id = NULL   | Pedido válido realizado por consumidor o grupo de consumidores no identificados. |

orders  
table_id = 8  
table_session_id = 310  
customer_id = NULL  
→ consumo de la mesa 8 sin identidad de cliente persistente

**CUS-03 Sin clientes técnicos.** No se DEBEN crear customers del tipo
CLIENTE_MESA_1, CONSUMIDOR_FINAL u otros registros artificiales para
satisfacer pedidos anónimos.

## 3.3 Relaciones que continúan requiriendo customer

En la versión actual del esquema, las siguientes entidades mantienen
customer_id NOT NULL:

| **Entidad**  | **Regla actual**                                                  |
|--------------|-------------------------------------------------------------------|
| reservations | La reserva debe estar asociada a un customer identificado. La barra queda fuera de este alcance (ver 6.4): no es una entidad reservable.|
| gift_cards   | La tarjeta regalo debe estar asociada a un customer identificado. card_number es único (la BD lo garantiza mediante UNIQUE). |
| reviews      | La reseña debe estar asociada a un customer identificado. rating está limitado a 1-5 mediante CHECK; comment es opcional.|

Estas restricciones son decisiones actuales del esquema. Permitir
reservas, gift cards o reseñas anónimas requeriría una modificación
posterior y no forma parte de esta versión.

# 4. Mesas: estado operativo y estado de uso

## 4.1 Responsabilidad de tables.operational_status

tables.operational_status describe exclusivamente si la mesa puede
utilizarse desde el punto de vista físico u operativo. Los únicos
valores válidos son:

| **Estado**     | **Significado**                                                                               |
|----------------|-----------------------------------------------------------------------------------------------|
| ACTIVE         | La mesa está físicamente habilitada. Su estado de uso se deriva mediante sesiones y reservas. |
| CLEANING       | La mesa está en limpieza y no puede asignarse a nuevos comensales.                            |
| OUT_OF_SERVICE | La mesa está fuera de servicio y no puede utilizarse.                                         |

**TBL-01 Separación de estados.** AVAILABLE, RESERVED y OCCUPIED NO
DEBEN almacenarse en tables.operational_status.

## 4.2 Estado de uso derivado por la aplicación

La aplicación calcula el estado visible de la mesa con la siguiente
precedencia:

1.  Si operational_status = OUT_OF_SERVICE → OUT_OF_SERVICE.

2.  Si operational_status = CLEANING → CLEANING.

3.  Si operational_status = ACTIVE y existe una table_session abierta →
    OCCUPIED.

4.  Si operational_status = ACTIVE, no hay sesión abierta y existe una
    reserva confirmada dentro de la ventana de reserva definida por el
    dominio → RESERVED.

5.  Si ninguna condición anterior aplica → AVAILABLE.

ACTIVE + sesión abierta = OCCUPIED  
ACTIVE + sin sesión + reserva vigente = RESERVED  
ACTIVE + sin sesión + sin reserva vigente = AVAILABLE  
CLEANING = CLEANING  
OUT_OF_SERVICE = OUT_OF_SERVICE

**TBL-02 Reserva temporal.** La aplicación DEBE definir la ventana
temporal en la que una reserva transforma la mesa en RESERVED. El
esquema no fija esa ventana.

## 4.3 Sesiones de mesa

table_sessions representa una ocupación real de una mesa. Una sesión
abierta tiene closed_at = NULL; una sesión cerrada tiene closed_at
informado.

| **Campo**   | **Uso**                                                       |
|-------------|---------------------------------------------------------------|
| table_id    | Mesa ocupada por la sesión.                                   |
| guest_count | Cantidad de comensales presentes; debe ser mayor que cero.    |
| opened_at   | Momento de inicio de ocupación.                               |
| closed_at   | Momento de cierre; NULL mientras la sesión permanece abierta. |

**SES-01 Una sesión abierta por mesa.** La BD garantiza, mediante índice
único parcial, que una mesa no puede tener más de una sesión con
closed_at IS NULL.

**SES-02 Apertura válida.** La aplicación DEBE impedir abrir una sesión
cuando la mesa no está ACTIVE.

**SES-03 Cierre válido.** closed_at DEBE ser igual o posterior a
opened_at. La BD protege esta condición mediante CHECK.

**SES-04 Cantidad de comensales.** guest_count representa personas
presentes, aunque ninguna de ellas sea un customer identificado.

## 4.4 Flujo de ocupación

Mesa ACTIVE / AVAILABLE  
│  
├─ llegan 4 personas  
▼  
crear table_session (guest_count = 4, closed_at = NULL)  
▼  
OCCUPIED  
│  
├─ se cierra la ocupación  
▼  
closed_at = timestamp  
▼  
si corresponde: CLEANING  
▼  
ACTIVE / AVAILABLE

El pago de la cuenta y la liberación física de la mesa no son
necesariamente el mismo evento. La aplicación PUEDE cerrar la sesión
cuando los comensales efectivamente abandonan la mesa y usar CLEANING
durante el acondicionamiento posterior.

## 4.5 tables representa exclusivamente espacios de salón

**TBL-03 Alcance de tables.** tables y table_sessions modelan
exclusivamente mesas de salón, donde la ocupación es exclusiva (una
sesión abierta por mesa, garantizada por índice único parcial) y donde
tiene sentido una reserva anticipada. La barra NO es una fila de
tables. La razón es de fondo, no de nomenclatura: la barra no tiene
ocupación exclusiva —varios clientes sin relación entre sí consumen
en ella al mismo tiempo— y el sistema no gestiona su espacio
disponible. Ver sección 5.4 para cómo se modela el consumo en barra y
6.4 para la exclusión explícita de reservas.

# 5. Pedidos, cuenta y relación con la mesa

## 5.1 Estructura del pedido

orders es la cabecera comercial del consumo. Se relaciona con sucursal,
mesa, empleado, cliente opcional y sesión de mesa opcional. order_items
contiene las líneas consumidas.

**ORD-01 Cliente opcional.** orders.customer_id PUEDE ser NULL.

**ORD-02 Sesión opcional en esquema.** orders.table_session_id es
nullable para conservar flexibilidad; cuando un pedido corresponde a
consumo en salón, la aplicación DEBERÍA asociarlo a la sesión abierta de
la mesa.

**ORD-03 Coherencia mesa-sesión.** Cuando table_session_id no sea NULL,
orders.table_id DEBE coincidir con table_sessions.table_id. Esta regla
no está implementada como constraint cruzado y debe validarla la
aplicación.

**ORD-04 Coherencia de sucursal.** orders.branch_id DEBE coincidir con
la sucursal de orders.table_id y con la sucursal operacional del
empleado que registra el pedido.

## 5.2 Cuenta por mesa y división de pagos

El sistema no necesita identificar a cada comensal ni asignar cada ítem
a una persona. La cuenta puede gestionarse colectivamente mediante el
pedido asociado a la mesa/sesión.

Mesa 12 / Session 550 / 4 comensales  
Order 900  
2 × Pizza  
1 × Pasta  
4 × Bebida  
customer_id = NULL

Los comensales pueden acordar por fuera del modelo cómo repartir la
cuenta. Si el restaurante desea registrar varios medios o fracciones de
pago para un mismo pedido, payments admite múltiples registros
vinculados al mismo order_id.

También es posible que una misma table_session tenga varios orders, por
ejemplo para separar cuentas. El esquema lo permite porque
table_session_id no es único en orders.

## 5.3 Cliente identificado durante una sesión

Si uno de los comensales desea asociar la compra a su identidad (por
fidelización, historial u otro motivo), customer_id puede referenciar
ese customer. Esto no implica que ese cliente sea necesariamente el
único consumidor de la mesa.

## 5.4 Canal de venta (orders.channel)

orders.channel clasifica cada pedido en uno de cuatro valores: MESA,
BARRA, TAKEAWAY o DELIVERY. A diferencia de otros estados del esquema,
este valor NO se deriva de otros hechos más fundamentales: MESA y
DELIVERY podrían inferirse de table_id y de la existencia de una fila
en deliveries respectivamente, pero BARRA y TAKEAWAY son
indistinguibles entre sí bajo esos mismos criterios (ninguno de los
dos tiene mesa ni registro de delivery). Por eso channel se almacena
de forma explícita y autoritativa para todo pedido, y no se reintroduce
como un caso más de estado derivado.

**ORD-05 Canal obligatorio.** Todo orders.channel DEBE tener uno de
los cuatro valores. La BD lo garantiza mediante CHECK.

**ORD-06 Coherencia canal-mesa.** Si channel = 'MESA', table_id DEBE
tener valor. Si channel es BARRA, TAKEAWAY o DELIVERY, table_id Y
table_session_id DEBEN ser NULL. La BD garantiza esta coherencia
mediante CHECK cruzado sobre las propias columnas de orders.

| **channel** | **table_id**  | **table_session_id** | **Significado**                                   |
|-------------|---------------|-----------------------|----------------------------------------------------|
| MESA        | Obligatorio   | Opcional (ORD-02)     | Consumo en una mesa de salón.                      |
| BARRA       | NULL          | NULL                  | Consumo en barra (ver 5.5).                        |
| TAKEAWAY    | NULL          | NULL                  | Para llevar, sin mesa ni sesión.                   |
| DELIVERY    | NULL          | NULL                  | Con reparto; debe existir fila en deliveries (DOM-08). |

## 5.5 Consumo en barra como caso especial

**BAR-01 La barra no es una mesa.** Un pedido con channel = 'BARRA' no
tiene table_id ni table_session_id. No existe ninguna fila de tables
que represente "la barra": no hay número, no hay capacidad, no hay
operational_status propio.

**BAR-02 Sin exclusividad de ocupación.** Varios pedidos con channel =
'BARRA' pueden coexistir en cualquier momento, generados por clientes
sin relación entre sí. La BD no impone (ni debe imponer) ningún límite
de cantidad simultánea, a diferencia de table_sessions para el salón.

**BAR-03 Sin gestión de espacio.** La aplicación NO DEBE implementar
validación de cupo, capacidad o disponibilidad de barra. La gestión
física del espacio de barra queda a cargo del personal del local, por
fuera del sistema.

**BAR-04 Barra y cliente identificado.** Igual que en salón (5.3), un
pedido de barra puede tener customer_id NULL o referenciar un customer
identificado, sin que eso implique exclusividad sobre el resto de los
clientes consumiendo en la barra en simultáneo.

**BAR-05 Barra no reservable.** Ver RSV-04 en la sección 6.4: al no
existir una fila de tables para la barra, una reserva de barra es
estructuralmente imposible, no solo desalentada por la aplicación.

# 6. Reservas y su interacción con mesas

reservations mantiene customer_id, branch_id, table_id,
reservation_time, party_size y status. En esta versión, una reserva
requiere customer identificado.

**RSV-01 Sucursal y mesa.** reservations.branch_id DEBE coincidir con la
sucursal de reservations.table_id. La aplicación debe validar esta
coherencia.

**RSV-02 Reserva no equivale a ocupación.** Una reservation no crea por
sí misma una table_session. La sesión se abre cuando los comensales
ocupan efectivamente la mesa.

**RSV-03 Estado RESERVED.** RESERVED es un estado derivado en la
aplicación a partir de reservation_time, reservation.status y la ventana
temporal configurada.

Cuando una reserva se presenta, el flujo recomendado es validar la
reserva, verificar que la mesa esté ACTIVE, abrir table_session con
guest_count real y luego considerar la mesa OCCUPIED.

## 6.4 La barra no es reservable

**RSV-04 Barra no reservable.** reservations.table_id solo puede
referenciar filas de tables, y tables representa exclusivamente
espacios de salón (TBL-03). Al no existir ninguna fila de tables que
represente la barra, no hay valor posible de reservations.table_id que
apunte a ella: la reserva de barra es imposible por construcción del
esquema, no una regla que la aplicación deba validar o pueda omitir
por error.

# 7. Inventario y materia prima

## 7.1 Catálogo de ingredientes

ingredients es un catálogo común de materias primas. Ya no contiene
stock_quantity. El saldo se separa por sucursal en branch_inventory.

**INV-01 Sin stock global.** La aplicación NO DEBE interpretar
ingredients como existencia física global. La existencia actual se
consulta en branch_inventory.

## 7.2 Saldo por sucursal

branch_inventory mantiene una fila por combinación branch_id +
ingredient_id y contiene stock_quantity. La BD impide duplicados
mediante UNIQUE y no permite saldos negativos mediante CHECK.

**INV-02 Registro único de saldo.** Debe existir como máximo un
branch_inventory por sucursal e ingrediente.

**INV-03 Saldo no negativo.** stock_quantity no puede ser menor que
cero.

## 7.3 Libro de movimientos

inventory_movements es el ledger de inventario. Cada variación debe
registrarse como movimiento firmado:

| **movement_type** | **Signo esperado**  | **Origen conceptual**                                              |
|-------------------|---------------------|--------------------------------------------------------------------|
| PURCHASE          | positivo            | Ingreso de mercadería efectivamente recibida.                      |
| SALE              | negativo            | Consumo de ingredientes derivado de productos vendidos/preparados. |
| WASTE             | negativo            | Merma o desperdicio registrado.                                    |
| ADJUSTMENT        | positivo o negativo | Corrección manual, inventario físico u otra regularización.        |

**INV-04 Trazabilidad.** Toda modificación de stock_quantity DEBE tener
un inventory_movement correspondiente, salvo la carga inicial/migración
expresamente documentada.

**INV-05 Atomicidad.** Insertar el movimiento y actualizar
branch_inventory DEBE ejecutarse dentro de la misma transacción.

**INV-06 Cantidad distinta de cero.** inventory_movements.quantity no
puede ser cero; la BD lo protege mediante CHECK.

## 7.4 Referencias de movimientos

reference_type y reference_id permiten enlazar lógicamente el movimiento
con su origen. Al ser una referencia polimórfica no tiene FK directa. Se
recomienda la siguiente convención de dominio:

| **movement_type** | **reference_type sugerido** | **reference_id**                                                              |
|-------------------|-----------------------------|-------------------------------------------------------------------------------|
| PURCHASE          | PURCHASE_ORDER              | purchase_orders.id o el identificador de recepción definido por la aplicación |
| SALE              | ORDER                       | orders.id                                                                     |
| WASTE             | WASTE_LOG                   | waste_logs.id                                                                 |
| ADJUSTMENT        | MANUAL_ADJUSTMENT           | Identificador de operación/auditoría si existe                                |

## 7.5 Consumo por receta

recipe_items define la cantidad requerida de cada ingrediente por
menu_item. La aplicación puede calcular el egreso de inventario
multiplicando quantity de order_items por quantity_required de
recipe_items. La BD garantiza, mediante UNIQUE (menu_item_id,
ingredient_id), que no existan filas duplicadas para el mismo par
plato-insumo; una duplicación haría ambiguo cuánto insumo descontar
por unidad vendida.

2 × Pizza Margarita  
recipe: mozzarella = 0.150 kg por pizza  
movimiento SALE = -(2 × 0.150) = -0.300 kg

El esquema no fija el instante exacto del ciclo del pedido en el que
debe generarse el movimiento SALE. La aplicación DEBE escoger un evento
de dominio único y consistente (por ejemplo, confirmación/preparación
definitiva), evitar duplicados y generar reversas/ajustes si una
operación ya contabilizada se cancela.

## 7.6 Compras y desperdicios

**INV-07 Compra.** Crear purchase_orders no debe aumentar stock por sí
solo. El movimiento PURCHASE debe generarse cuando el ingreso de
mercadería sea efectivo según la regla de recepción de la aplicación.

**INV-08 Merma.** Un waste_log confirmado debe originar un movimiento
WASTE equivalente en la misma sucursal e ingrediente.

# 8. Métricas de ventas y operación

El esquema no almacena métricas agregadas como entidades separadas. Las
métricas se derivan de los datos transaccionales.

| **Métrica**                     | **Fuente principal**                                                |
|---------------------------------|---------------------------------------------------------------------|
| Ventas por período              | orders.order_time + orders.total_amount + criterio de orders.status |
| Cantidad de pedidos             | orders                                                              |
| Ticket promedio                 | AVG(orders.total_amount) sobre pedidos contabilizados               |
| Unidades/productos más vendidos | order_items + menu_items                                            |
| Ventas por categoría            | order_items + menu_items + categories                               |
| Ventas por sucursal             | orders.branch_id                                                    |
| Ventas por empleado             | orders.employee_id                                                  |
| Ventas por canal                | orders.channel (MESA/BARRA/TAKEAWAY/DELIVERY)                       |
| Medios de pago                  | payments                                                            |
| Descuentos aplicados            | order_discounts + discounts                                         |
| Stock actual                    | branch_inventory                                                    |
| Entradas/salidas/mermas         | inventory_movements                                                 |
| Costo de compras                | purchase_order_items                                                |
| Rotación/ocupación de mesas     | table_sessions.opened_at/closed_at/guest_count                      |

**MET-01 Pedidos contabilizados.** La aplicación/consulta analítica DEBE
definir qué valores de orders.status representan ventas efectivas y
excluir borradores, cancelaciones u otros estados no vendidos.

**MET-02 Fuente de stock.** Las métricas históricas de movimientos deben
provenir de inventory_movements; el stock actual debe provenir de
branch_inventory.

# 9. Responsabilidades: aplicación vs. base de datos

| **Responsabilidad de la BD**                                          | **Responsabilidad de la aplicación/dominio**                                                      |
|-----------------------------------------------------------------------|---------------------------------------------------------------------------------------------------|
| PK/FK e integridad referencial.                                       | Decidir cuándo abrir/cerrar sesiones y validar transiciones.                                      |
| CHECK de operational_status.                                          | Derivar AVAILABLE / RESERVED / OCCUPIED.                                                          |
| Una sola sesión abierta por mesa.                                     | Validar que la mesa esté ACTIVE antes de abrir sesión.                                            |
| guest_count \> 0 y closed_at \>= opened_at.                           | Aplicar la ventana temporal de reservas.                                                          |
| Un branch_inventory por sucursal/ingrediente.                         | Registrar movimiento + saldo de inventario de forma atómica.                                      |
| stock_quantity \>= 0.                                                 | Evitar doble contabilización e implementar reversas cuando corresponda.                           |
| movement_type válido y quantity != 0.                                 | Validar coherencia entre branch_id, table_id, session_id, employee_id y referencias polimórficas. |
| customer_id de orders puede ser NULL y, si tiene valor, debe existir. | Decidir cuándo asociar un customer identificado a un pedido.                                      |
| channel válido y coherente con table_id/table_session_id (CHECK).    | No implementar gestión de cupo/capacidad para pedidos BARRA (BAR-03).                              |
| Unicidad de (branch_id, number) en tables, card_number en gift_cards, y de las combinaciones de las tablas puente (recipe_items, station_menu_items, menu_item_taxes, order_discounts). | Garantizar coherencia canal-delivery (DOM-08): toda orden DELIVERY tiene su fila en deliveries y viceversa. |

La estrategia deliberadamente evita usar triggers como mecanismo
principal de workflow. La aplicación expresa la lógica de negocio de
forma explícita y testeable; la BD actúa como última barrera de
consistencia.

# 10. Invariantes de consistencia de dominio

Además de los constraints ya presentes en SQL, la aplicación DEBE
proteger las siguientes invariantes cruzadas:

**DOM-01 Pedido y mesa.** orders.branch_id debe coincidir con
tables.branch_id.

**DOM-02 Pedido y sesión.** Si orders.table_session_id tiene valor, la
sesión debe pertenecer a orders.table_id.

**DOM-03 Sesión activa.** No se puede abrir table_session sobre una mesa
CLEANING u OUT_OF_SERVICE.

**DOM-04 Reserva y mesa.** reservations.branch_id debe coincidir con
tables.branch_id.

**DOM-05 Inventario.** inventory_movements.branch_id e ingredient_id
deben corresponder al branch_inventory modificado.

**DOM-06 Empleado y operación.** Las operaciones de sucursal deben
validar que el empleado esté habilitado para operar en esa sucursal
según las reglas de la aplicación.

**DOM-07 Idempotencia de stock.** Una misma operación origen no debe
generar el mismo movimiento de inventario más de una vez.

**DOM-08 Coherencia canal-delivery.** Si orders.channel = 'DELIVERY',
debe existir una fila correspondiente en deliveries con ese order_id,
y viceversa. No se modela con FK inversa ni trigger, siguiendo el
enfoque application-driven de la sección 9.

**DOM-09 Barra sin gestión de espacio.** La aplicación NO debe
implementar ninguna validación de capacidad, cupo o exclusividad para
pedidos con channel = 'BARRA'. A diferencia de las mesas, la barra no
tiene fila en tables ni sesión en table_sessions.

**DOM-10 Barra no reservable.** Ver RSV-04: no existe valor de
reservations.table_id que represente la barra, por lo que su
exclusión de las reservas es estructural y no depende de una
validación de aplicación.

# 11. Flujos operativos de referencia

## 11.1 Mesa con consumidores anónimos

6.  Verificar que tables.operational_status = ACTIVE.

7.  Abrir table_session con guest_count real.

8.  Crear order asociado a table_id y table_session_id con customer_id =
    NULL.

9.  Agregar order_items durante el consumo.

10. Registrar uno o más payments según corresponda.

11. Cerrar el pedido según el workflow comercial.

12. Cerrar la table_session cuando la mesa queda efectivamente
    desocupada.

13. Si requiere acondicionamiento, marcar operational_status = CLEANING
    y posteriormente volver a ACTIVE.

## 11.2 Mesa con cliente identificado

El flujo es el mismo, excepto que orders.customer_id referencia al
customer identificado. guest_count puede ser mayor que uno: customer_id
identifica la relación comercial, no necesariamente a todos los
comensales.

## 11.3 Llegada de una reserva

14. Localizar reservation confirmada y verificar la mesa asociada.

15. Validar que la mesa esté ACTIVE y que no exista sesión abierta
    incompatible.

16. Abrir table_session con el número real de comensales presentes.

17. Crear el/los orders necesarios y asociarlos a la sesión.

18. La existencia de la sesión hace que el estado derivado pase de
    RESERVED a OCCUPIED.

## 11.4 Recepción de compra

19. Confirmar la recepción según purchase_order/purchase_order_items.

20. Por cada ingrediente recibido, insertar inventory_movement tipo
    PURCHASE con cantidad positiva.

21. Actualizar branch_inventory en la misma transacción.

22. Si cualquier actualización produciría una inconsistencia, revertir
    toda la transacción.

## 11.5 Venta y consumo de ingredientes

23. Determinar las líneas de order_items que alcanzaron el evento de
    dominio elegido para descontar stock.

24. Expandir cada menu_item mediante recipe_items.

25. Agrupar cantidades por ingredient_id para evitar movimientos
    redundantes si se desea.

26. Insertar movimientos SALE negativos y actualizar branch_inventory
    atómicamente.

27. Guardar reference_type = ORDER y reference_id = orders.id para
    trazabilidad.

## 11.6 Desperdicio

28. Crear/confirmar waste_log con motivo, cantidad, ingrediente,
    sucursal y empleado.

29. Crear inventory_movement WASTE por la misma cantidad con signo
    negativo.

30. Actualizar branch_inventory en la misma transacción.

## 11.7 Consumo en barra

31. Crear order con channel = 'BARRA', table_id = NULL,
    table_session_id = NULL, y customer_id según corresponda (5.3,
    BAR-04).

32. Agregar order_items durante el consumo, igual que en salón.

33. Registrar uno o más payments según corresponda.

34. Cerrar el pedido según el workflow comercial. No hay paso
    equivalente a "cerrar table_session": la barra nunca tuvo una
    sesión que cerrar (BAR-01).

35. La aplicación NO valida cupo disponible en ningún paso de este
    flujo (BAR-03): puede haber tantos pedidos BARRA abiertos en
    simultáneo como el personal del local permita físicamente.

# 12. Consultas conceptuales recomendadas

Los siguientes ejemplos son conceptuales y deben adaptarse al catálogo
de estados definitivo de la aplicación.

## 12.1 Mesas ocupadas

SELECT t.id, t.number, ts.id AS session_id, ts.guest_count  
FROM tables t  
JOIN table_sessions ts ON ts.table_id = t.id  
WHERE t.operational_status = 'ACTIVE'  
AND ts.closed_at IS NULL;

## 12.2 Stock actual por sucursal

SELECT bi.branch_id, i.id AS ingredient_id, i.name, i.unit,
bi.stock_quantity  
FROM branch_inventory bi  
JOIN ingredients i ON i.id = bi.ingredient_id  
WHERE bi.branch_id = :branch_id;

## 12.3 Historial de movimientos de un ingrediente

SELECT movement_time, movement_type, quantity, reference_type,
reference_id  
FROM inventory_movements  
WHERE branch_id = :branch_id  
AND ingredient_id = :ingredient_id  
ORDER BY movement_time, id;

## 12.4 Pedidos anónimos

SELECT \*  
FROM orders  
WHERE customer_id IS NULL;

## 12.5 Ventas por canal

SELECT channel, COUNT(\*) AS pedidos, SUM(total_amount) AS total  
FROM orders  
GROUP BY channel;

## 12.6 Pedidos de barra abiertos

SELECT id, employee_id, customer_id, order_time, total_amount  
FROM orders  
WHERE channel = 'BARRA'  
AND status NOT IN ('CLOSED', 'CANCELLED');

# 13. Decisiones implementadas en esta versión

| **Decisión**               | **Resultado**                                                                   |
|----------------------------|---------------------------------------------------------------------------------|
| Consumidor anónimo         | orders.customer_id permite NULL.                                                |
| Stock multisucursal        | ingredients es catálogo; branch_inventory contiene el saldo por sucursal.       |
| Trazabilidad de inventario | inventory_movements registra PURCHASE, SALE, WASTE y ADJUSTMENT.                |
| Estado de mesa             | tables.operational_status solo expresa ACTIVE, CLEANING u OUT_OF_SERVICE.       |
| Ocupación                  | table_sessions representa la ocupación real y conserva guest_count e historial. |
| Estado de uso              | AVAILABLE, RESERVED y OCCUPIED son derivados por la aplicación.                 |
| Integridad de ocupación    | La BD impide dos sesiones abiertas simultáneamente para una misma mesa.         |
| Relación pedido-sesión     | orders.table_session_id permite vincular consumo con una ocupación concreta.    |
| Delivery sin hora real (v2)     | deliveries.actual_time es nullable; se completa recién al concretarse la entrega.|
| Pedido sin mesa física (v2)     | orders.table_id es nullable, habilitando delivery, para llevar y barra.        |
| Unicidad de mesa por sucursal (v2) | tables tiene UNIQUE (branch_id, number).                                    |
| Unicidad de tarjeta de regalo (v2) | gift_cards.card_number es UNIQUE.                                            |
| Unicidad en tablas puente (v2)  | UNIQUE en recipe_items, station_menu_items, menu_item_taxes y order_discounts sobre sus claves compuestas. |
| Rating válido (v2)              | reviews.rating restringido a 1-5 mediante CHECK.                            |
| Campos de texto opcionales (v2) | order_items.notes y reviews.comment dejaron de ser NOT NULL.                |
| Generación de IDs (v2)          | Todas las PK usan GENERATED BY DEFAULT AS IDENTITY.                         |
| Canal de venta (v3)             | orders.channel (MESA/BARRA/TAKEAWAY/DELIVERY) es explícito y autoritativo.  |
| Consumo en barra (v3)           | Modelado como channel = 'BARRA' en orders, sin fila en tables ni en table_sessions; sin gestión de cupo; no reservable por construcción. |

# 14. Aspectos deliberadamente no resueltos por el esquema

Las siguientes políticas quedan a cargo del dominio o de una futura
iteración del modelo:

- Duración exacta de la ventana temporal que convierte una reserva en
  RESERVED.

- Catálogo exhaustivo y transiciones de status de orders, reservations,
  payments, deliveries y otras entidades que aún usan varchar.

- Momento exacto del workflow del pedido en el que se descuenta stock y
  política de reversa ante cancelaciones.

- Permitir o no reservas, gift cards o reviews sin customer
  identificado; actualmente siguen requiriéndolo.

- Materialización de métricas agregadas o data warehouse; actualmente se
  calculan desde datos transaccionales.

- Uso de triggers. La versión actual adopta explícitamente un enfoque
  application-driven para el workflow.

- Agrupación de varios pedidos BARRA bajo una misma "cuenta" o tab. El
  esquema no define ese concepto: cada pedido de barra es
  independiente. Si el negocio necesita abrir una cuenta de barra que
  acumule varios pedidos antes del pago, es una decisión de una futura
  iteración.

- Mecanismo de enforcement de DOM-08 (coherencia channel='DELIVERY' ↔
  fila en deliveries). Hoy es responsabilidad exclusiva de la
  aplicación; no hay CHECK ni trigger que lo valide en la BD.

# 15. Checklist de implementación

- **☐** Backend acepta customer_id = NULL al crear pedidos anónimos.

- **☐** Backend nunca crea clientes ficticios por mesa.

- **☐** Vista de salón calcula AVAILABLE/RESERVED/OCCUPIED sin escribir
  esos valores en tables.

- **☐** Abrir una mesa crea table_session y respeta la restricción de
  una sesión abierta.

- **☐** Cerrar una ocupación informa closed_at.

- **☐** orders de salón se vinculan a la sesión correspondiente siempre
  que sea posible.

- **☐** Stock se consulta desde branch_inventory y no desde ingredients.

- **☐** Toda variación de stock genera inventory_movements.

- **☐** Movimiento y actualización de saldo se ejecutan atómicamente.

- **☐** Consultas de ventas filtran correctamente los estados que
  representan ventas efectivas.

- **☐** Se validan las coherencias de sucursal/mesa/sesión que no están
  cubiertas por FK simples.

- **☐** Todo pedido se crea con un channel explícito (MESA, BARRA,
  TAKEAWAY o DELIVERY).

- **☐** Backend nunca asigna table_id ni table_session_id a un pedido
  con channel distinto de MESA.

- **☐** Backend no implementa validación de cupo/capacidad para
  pedidos de channel BARRA.

- **☐** Backend garantiza que toda orden DELIVERY tenga su fila
  correspondiente en deliveries (DOM-08).

- **☐** La UI de reservas nunca ofrece la barra como opción, porque no
  existe una fila de tables que la represente.

# Anexo A. Relación entre hechos persistidos y estados derivados

| **Concepto**                       | **Persistido en BD**                          | **Derivado por app**            |
|------------------------------------|-----------------------------------------------|---------------------------------|
| Mesa físicamente habilitada        | tables.operational_status = ACTIVE            | No                              |
| Mesa en limpieza/fuera de servicio | tables.operational_status                     | No                              |
| Mesa ocupada                       | table_sessions con closed_at NULL             | OCCUPIED                        |
| Mesa reservada                     | reservations + hora/status                    | RESERVED                        |
| Mesa disponible                    | Ausencia de sesión/reserva aplicable + ACTIVE | AVAILABLE                       |
| Cliente identificado               | customers + orders.customer_id                | No                              |
| Consumidor anónimo                 | orders.customer_id = NULL                     | Interpretación del NULL         |
| Stock actual                       | branch_inventory.stock_quantity               | No                              |
| Historia de stock                  | inventory_movements                           | No                              |
| Ventas agregadas                   | orders/order_items/payments                   | Sí, mediante consulta/analytics |
| Canal de venta (MESA/BARRA/TAKEAWAY/DELIVERY) | orders.channel                     | No — es autoritativo, no derivado (ver 5.4) |

# Anexo B. Regla de precedencia para visualización de mesa

function tableDisplayStatus(table, now):  
if table.operational_status == OUT_OF_SERVICE:  
return OUT_OF_SERVICE  
  
if table.operational_status == CLEANING:  
return CLEANING  
  
if existsOpenTableSession(table.id):  
return OCCUPIED  
  
if existsReservationInConfiguredWindow(table.id, now):  
return RESERVED  
  
return AVAILABLE

Esta función nunca se invoca para la barra: al no existir una fila de
tables que la represente, no tiene un estado de uso (AVAILABLE/
RESERVED/OCCUPIED) que calcular. La barra solo se refleja en el
sistema a través de sus pedidos (orders.channel = 'BARRA'), no a
través de un estado de ocupación de espacio.

Fin de la especificación.
