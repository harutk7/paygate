import { AxiosInstance } from 'axios';
import { OrganizationDto, UpdateOrganizationRequest } from '@payment-gateway/types';

export function createOrganizationsApi(client: AxiosInstance) {
  return {
    getMyOrg: () =>
      client.get<OrganizationDto>('/api/organizations/me').then((r) => r.data),

    updateOrg: (data: UpdateOrganizationRequest) =>
      client.put<OrganizationDto>('/api/organizations/me', data).then((r) => r.data),
  };
}
