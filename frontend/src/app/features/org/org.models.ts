export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface Restaurant {
  id: number;
  name: string;
  address: string;
  phone: string;
  email: string;
  taxNumber: string;
}

export interface Branch {
  id: number;
  restaurantId: number;
  name: string;
  address: string;
  phone: string;
  email: string;
  openingTime: string; // "HH:mm:ss"
  closingTime: string;
}

export interface Role {
  id: number;
  name: string;
  description: string;
  hourlyRate: number;
}

export interface Department {
  id: number;
  branchId: number;
  name: string;
  description: string;
}

export interface Employee {
  id: number;
  branchId: number;
  departmentId: number;
  roleId: number;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  hireDate: string; // "yyyy-MM-dd"
}

export interface Shift {
  id: number;
  employeeId: number;
  startTime: string;
  endTime: string;
  scheduledHours: number;
}

export interface Leave {
  id: number;
  employeeId: number;
  startDate: string;
  endDate: string;
  leaveType: string;
  status: string;
}

export const LEAVE_TYPES = ['VACATION', 'SICK', 'UNPAID', 'OTHER'] as const;
