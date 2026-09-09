using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RestoManager.Business.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    loyalty_points = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discounts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ingredients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "restaurants",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tax_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_restaurants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    hourly_rate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    contact_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tax_rates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tax_rates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "menu_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_items_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "gift_cards",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    card_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    balance = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gift_cards", x => x.id);
                    table.ForeignKey(
                        name: "fk_gift_cards_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    restaurant_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    opening_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    closing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branches", x => x.id);
                    table.ForeignKey(
                        name: "fk_branches_restaurants_restaurant_id",
                        column: x => x.restaurant_id,
                        principalTable: "restaurants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "menu_item_taxes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    menu_item_id = table.Column<int>(type: "integer", nullable: false),
                    tax_rate_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_item_taxes", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_item_taxes_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalTable: "menu_items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_menu_item_taxes_tax_rates_tax_rate_id",
                        column: x => x.tax_rate_id,
                        principalTable: "tax_rates",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "recipe_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    menu_item_id = table.Column<int>(type: "integer", nullable: false),
                    ingredient_id = table.Column<int>(type: "integer", nullable: false),
                    quantity_required = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_items_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_recipe_items_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalTable: "menu_items",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "branch_inventory",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    ingredient_id = table.Column<int>(type: "integer", nullable: false),
                    stock_quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branch_inventory", x => x.id);
                    table.CheckConstraint("ck_branch_inventory_stock_non_negative", "stock_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_branch_inventory_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_branch_inventory_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                    table.ForeignKey(
                        name: "fk_departments_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "kitchen_stations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kitchen_stations", x => x.id);
                    table.ForeignKey(
                        name: "fk_kitchen_stations_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_orders_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_orders_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    review_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviews", x => x.id);
                    table.CheckConstraint("ck_reviews_rating", "rating BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_reviews_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_reviews_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tables",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    operational_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tables", x => x.id);
                    table.CheckConstraint("ck_tables_operational_status", "operational_status IN ('ACTIVE', 'CLEANING', 'OUT_OF_SERVICE')");
                    table.ForeignKey(
                        name: "fk_tables_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    department_id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hire_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.id);
                    table.ForeignKey(
                        name: "fk_employees_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_employees_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_employees_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "station_menu_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    station_id = table.Column<int>(type: "integer", nullable: false),
                    menu_item_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_station_menu_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_station_menu_items_kitchen_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "kitchen_stations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_station_menu_items_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalTable: "menu_items",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    ingredient_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_order_items_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_order_items_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reservations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    table_id = table.Column<int>(type: "integer", nullable: false),
                    reservation_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    party_size = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservations", x => x.id);
                    table.ForeignKey(
                        name: "fk_reservations_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_reservations_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_reservations_tables_table_id",
                        column: x => x.table_id,
                        principalTable: "tables",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "table_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    table_id = table.Column<int>(type: "integer", nullable: false),
                    guest_count = table.Column<int>(type: "integer", nullable: false),
                    opened_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_table_sessions", x => x.id);
                    table.CheckConstraint("ck_table_sessions_closed_after_opened", "closed_at IS NULL OR closed_at >= opened_at");
                    table.CheckConstraint("ck_table_sessions_guest_count", "guest_count > 0");
                    table.ForeignKey(
                        name: "fk_table_sessions_tables_table_id",
                        column: x => x.table_id,
                        principalTable: "tables",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "delivery_drivers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    vehicle_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    license_plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_drivers", x => x.id);
                    table.ForeignKey(
                        name: "fk_delivery_drivers_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "employee_leaves",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    leave_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_leaves", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_leaves_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    ingredient_id = table.Column<int>(type: "integer", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    movement_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<int>(type: "integer", nullable: true),
                    employee_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movements", x => x.id);
                    table.CheckConstraint("ck_inventory_movements_quantity", "quantity <> 0");
                    table.CheckConstraint("ck_inventory_movements_type", "movement_type IN ('PURCHASE', 'SALE', 'WASTE', 'ADJUSTMENT')");
                    table.ForeignKey(
                        name: "fk_inventory_movements_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_inventory_movements_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_inventory_movements_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "shifts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    scheduled_hours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shifts", x => x.id);
                    table.ForeignKey(
                        name: "fk_shifts_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "waste_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    ingredient_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    logged_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    logged_by = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waste_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_waste_logs_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_waste_logs_employees_logged_by",
                        column: x => x.logged_by,
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_waste_logs_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    table_id = table.Column<int>(type: "integer", nullable: true),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    table_session_id = table.Column<int>(type: "integer", nullable: true),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.CheckConstraint("ck_orders_channel", "channel IN ('MESA', 'BARRA', 'TAKEAWAY', 'DELIVERY')");
                    table.CheckConstraint("ck_orders_channel_table", "(channel = 'MESA' AND table_id IS NOT NULL) OR (channel <> 'MESA' AND table_id IS NULL AND table_session_id IS NULL)");
                    table.ForeignKey(
                        name: "fk_orders_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_orders_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_orders_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_orders_table_sessions_table_session_id",
                        column: x => x.table_session_id,
                        principalTable: "table_sessions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_orders_tables_table_id",
                        column: x => x.table_id,
                        principalTable: "tables",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "deliveries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    driver_id = table.Column<int>(type: "integer", nullable: false),
                    delivery_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    estimated_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    actual_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_deliveries_delivery_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "delivery_drivers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_deliveries_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "gift_card_transactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gift_card_id = table.Column<int>(type: "integer", nullable: false),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    transaction_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gift_card_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_gift_card_transactions_gift_cards_gift_card_id",
                        column: x => x.gift_card_id,
                        principalTable: "gift_cards",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_gift_card_transactions_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "order_discounts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    discount_id = table.Column<int>(type: "integer", nullable: false),
                    applied_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_discounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_discounts_discounts_discount_id",
                        column: x => x.discount_id,
                        principalTable: "discounts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_order_discounts_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    menu_item_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_items_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalTable: "menu_items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    payment_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_branch_inventory_branch_id_ingredient_id",
                table: "branch_inventory",
                columns: new[] { "branch_id", "ingredient_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_branch_inventory_ingredient_id",
                table: "branch_inventory",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_branches_restaurant_id",
                table: "branches",
                column: "restaurant_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_driver_id",
                table: "deliveries",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_order_id",
                table: "deliveries",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_drivers_employee_id",
                table: "delivery_drivers",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_departments_branch_id",
                table: "departments",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_leaves_employee_id",
                table: "employee_leaves",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_branch_id",
                table: "employees",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_department_id",
                table: "employees",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_role_id",
                table: "employees",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_gift_card_transactions_gift_card_id",
                table: "gift_card_transactions",
                column: "gift_card_id");

            migrationBuilder.CreateIndex(
                name: "ix_gift_card_transactions_order_id",
                table: "gift_card_transactions",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_gift_cards_card_number",
                table: "gift_cards",
                column: "card_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gift_cards_customer_id",
                table: "gift_cards",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_branch_id_ingredient_id_movement_time",
                table: "inventory_movements",
                columns: new[] { "branch_id", "ingredient_id", "movement_time" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_employee_id",
                table: "inventory_movements",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_ingredient_id",
                table: "inventory_movements",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_reference_type_reference_id",
                table: "inventory_movements",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "ix_kitchen_stations_branch_id",
                table: "kitchen_stations",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_taxes_menu_item_id_tax_rate_id",
                table: "menu_item_taxes",
                columns: new[] { "menu_item_id", "tax_rate_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_taxes_tax_rate_id",
                table: "menu_item_taxes",
                column: "tax_rate_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_category_id",
                table: "menu_items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_discounts_discount_id",
                table: "order_discounts",
                column: "discount_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_discounts_order_id_discount_id",
                table: "order_discounts",
                columns: new[] { "order_id", "discount_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_items_menu_item_id",
                table: "order_items",
                column: "menu_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_branch_id",
                table: "orders",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_customer_id",
                table: "orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_employee_id",
                table: "orders",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_table_id",
                table: "orders",
                column: "table_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_table_session_id",
                table: "orders",
                column: "table_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_order_id",
                table: "payments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_items_ingredient_id",
                table: "purchase_order_items",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_items_purchase_order_id",
                table: "purchase_order_items",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_branch_id",
                table: "purchase_orders",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_supplier_id",
                table: "purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_items_ingredient_id",
                table: "recipe_items",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_items_menu_item_id_ingredient_id",
                table: "recipe_items",
                columns: new[] { "menu_item_id", "ingredient_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reservations_branch_id",
                table: "reservations",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_customer_id",
                table: "reservations",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_table_id",
                table: "reservations",
                column: "table_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_branch_id",
                table: "reviews",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_customer_id",
                table: "reviews",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_shifts_employee_id",
                table: "shifts",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_station_menu_items_menu_item_id",
                table: "station_menu_items",
                column: "menu_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_station_menu_items_station_id_menu_item_id",
                table: "station_menu_items",
                columns: new[] { "station_id", "menu_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_table_sessions_one_open_per_table",
                table: "table_sessions",
                column: "table_id",
                unique: true,
                filter: "closed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tables_branch_id_number",
                table: "tables",
                columns: new[] { "branch_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_waste_logs_branch_id",
                table: "waste_logs",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_waste_logs_ingredient_id",
                table: "waste_logs",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_waste_logs_logged_by",
                table: "waste_logs",
                column: "logged_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_inventory");

            migrationBuilder.DropTable(
                name: "deliveries");

            migrationBuilder.DropTable(
                name: "employee_leaves");

            migrationBuilder.DropTable(
                name: "gift_card_transactions");

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "menu_item_taxes");

            migrationBuilder.DropTable(
                name: "order_discounts");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "purchase_order_items");

            migrationBuilder.DropTable(
                name: "recipe_items");

            migrationBuilder.DropTable(
                name: "reservations");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "shifts");

            migrationBuilder.DropTable(
                name: "station_menu_items");

            migrationBuilder.DropTable(
                name: "waste_logs");

            migrationBuilder.DropTable(
                name: "delivery_drivers");

            migrationBuilder.DropTable(
                name: "gift_cards");

            migrationBuilder.DropTable(
                name: "tax_rates");

            migrationBuilder.DropTable(
                name: "discounts");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "purchase_orders");

            migrationBuilder.DropTable(
                name: "kitchen_stations");

            migrationBuilder.DropTable(
                name: "menu_items");

            migrationBuilder.DropTable(
                name: "ingredients");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "table_sessions");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "tables");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "restaurants");
        }
    }
}
