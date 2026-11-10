export interface ManagedUser { 
    id: string; 
    email: string; 
    isActive: boolean;
    employeeId: string | null; 
    roles: string[] 
}
export interface AuditEvent { 
    id: string; 
    occurredAtUtc: string; 
    userId: string | null; 
    employeeId: string | null; 
    actorEmail: string | null; 
    action: string; 
    entityType: string; 
    entityId: string; 
    correlationId: string | null; 
    ipAddress: string | null 
}
