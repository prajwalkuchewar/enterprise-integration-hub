export interface ListResponse<T> {
  items: T[];
  totalCount: number;
}

export interface ExternalSystem {
  id: string;
  name: string;
  description: string;
  environment: number;
  status: number;
}

export interface Connector {
  id: string;
  name: string;
  description: string;
  externalSystemId: string;
  baseUrl: string;
  protocol: number;
  authenticationType: number;
  timeoutSeconds: number;
  status: number;
}

export interface Workflow {
  id: string;
  name: string;
  sourceConnectorId: string;
  triggerEvent: string;
  status: number;
  stepCount: number;
}

export interface WorkflowDetails extends Omit<Workflow, 'stepCount'> {
  steps: WorkflowStep[];
}

export interface WorkflowStep {
  destinationConnectorId: string;
  executionOrder: number;
}

export interface ExternalSystemRequest {
  name: string;
  description: string;
  environment: number;
}

export interface ConnectorRequest {
  name: string;
  description: string;
  externalSystemId?: string;
  baseUrl: string;
  protocol: number;
  authenticationType: number;
  timeoutSeconds: number;
}

export interface WorkflowRequest {
  name: string;
  sourceConnectorId: string;
  triggerEvent: string;
  steps: WorkflowStep[];
}

export const environments = [
  { value: 1, label: 'Development' },
  { value: 2, label: 'Testing' },
  { value: 3, label: 'User acceptance' },
  { value: 4, label: 'Production' }
];

export const protocols = [
  { value: 1, label: 'REST' },
  { value: 2, label: 'SOAP' },
  { value: 3, label: 'SFTP' },
  { value: 4, label: 'GraphQL' }
];

export const authenticationTypes = [
  { value: 1, label: 'API key' },
  { value: 2, label: 'OAuth 2.0' },
  { value: 3, label: 'Basic auth' },
  { value: 4, label: 'Bearer token' }
];

export function environmentLabel(value: number): string {
  return environments.find((item) => item.value === value)?.label ?? 'Unknown';
}

export function protocolLabel(value: number): string {
  return protocols.find((item) => item.value === value)?.label ?? 'Unknown';
}

export function authenticationLabel(value: number): string {
  return authenticationTypes.find((item) => item.value === value)?.label ?? 'Unknown';
}

export function statusLabel(kind: 'system' | 'connector' | 'workflow', value: number): string {
  if (kind === 'system') return value === 1 ? 'Active' : 'Inactive';
  if (kind === 'connector') return ({ 1: 'Draft', 2: 'Active', 3: 'Inactive' } as Record<number, string>)[value] ?? 'Unknown';
  return ({ 0: 'Draft', 1: 'Active', 2: 'Inactive' } as Record<number, string>)[value] ?? 'Unknown';
}
