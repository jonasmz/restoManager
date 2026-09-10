import type {
  ApexAxisChartSeries, ApexChart, ApexDataLabels, ApexFill, ApexLegend, ApexPlotOptions,
  ApexStroke, ApexTooltip, ApexXAxis, ApexYAxis,
} from 'ng-apexcharts';

// Paleta del template InApp (assets/js/chart.js).
export const CHART_COLORS = {
  salesLight: '#f7a085',
  sales: '#E66239',
  purchase: '#E66239',
  green: '#5BE49B',
  greenDark: '#198754',
  grid: '#e2e8f0',
} as const;

export interface BarChartView {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  colors: string[];
  xaxis: ApexXAxis;
  yaxis: ApexYAxis;
  dataLabels: ApexDataLabels;
  legend: ApexLegend;
  grid: { borderColor: string };
  plotOptions: ApexPlotOptions;
  stroke: ApexStroke;
  tooltip: ApexTooltip;
}

export interface AreaChartView extends Omit<BarChartView, 'plotOptions'> {
  fill: ApexFill;
}

const baseChart = (type: ApexChart['type'], height = 320): ApexChart => ({
  type,
  height,
  fontFamily: 'inherit',
  parentHeightOffset: 0,
  toolbar: { show: false },
  animations: { enabled: true },
});

const commonLegend: ApexLegend = { show: true, position: 'top', horizontalAlign: 'right', fontWeight: 500 };
const noDataLabels: ApexDataLabels = { enabled: false };

export function barChart(
  categories: string[], series: ApexAxisChartSeries, colors: string[], currencyTooltip = true,
): BarChartView {
  return {
    series,
    chart: baseChart('bar'),
    colors,
    xaxis: { categories, labels: { style: { colors: '#64748b' } } },
    yaxis: { labels: { style: { colors: '#64748b' }, formatter: (v: number) => formatCompact(v) } },
    dataLabels: noDataLabels,
    legend: commonLegend,
    grid: { borderColor: CHART_COLORS.grid },
    plotOptions: { bar: { borderRadius: 4, columnWidth: '55%' } },
    stroke: { show: true, width: 2, colors: ['transparent'] },
    tooltip: currencyTooltip
      ? { y: { formatter: (v: number) => formatMoney(v) } }
      : {},
  };
}

export function areaChart(
  categories: string[], series: ApexAxisChartSeries, colors: string[],
): AreaChartView {
  return {
    series,
    chart: baseChart('area'),
    colors,
    xaxis: { categories, labels: { style: { colors: '#64748b' } } },
    yaxis: { labels: { style: { colors: '#64748b' }, formatter: (v: number) => formatCompact(v) } },
    dataLabels: noDataLabels,
    legend: commonLegend,
    grid: { borderColor: CHART_COLORS.grid },
    stroke: { curve: 'smooth', width: 2 },
    fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.4, opacityTo: 0.05 } },
    tooltip: { y: { formatter: (v: number) => formatMoney(v) } },
  };
}

function formatCompact(v: number): string {
  if (Math.abs(v) >= 1_000_000) {
    return (v / 1_000_000).toFixed(1) + 'M';
  }
  if (Math.abs(v) >= 1_000) {
    return (v / 1_000).toFixed(1) + 'k';
  }
  return String(Math.round(v));
}

function formatMoney(v: number): string {
  return '$ ' + v.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
