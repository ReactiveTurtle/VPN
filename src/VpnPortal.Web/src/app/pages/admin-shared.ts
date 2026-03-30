export interface AdminSectionLink {
  route: string;
  label: string;
  description: string;
}

export const ADMIN_SECTION_LINKS: AdminSectionLink[] = [
  {
    route: '/admin/requests/pending',
    label: 'Ожидающие заявки',
    description: 'Очередь модерации новых запросов на доступ.'
  },
  {
    route: '/admin/requests/history',
    label: 'История решений',
    description: 'Одобренные и отклоненные заявки с итогом обработки.'
  },
  {
    route: '/admin/audit',
    label: 'Аудит',
    description: 'Журнал действий пользователей, администраторов и системы.'
  },
  {
    route: '/admin/sessions',
    label: 'Сессии',
    description: 'Активные и недавние VPN-подключения.'
  },
  {
    route: '/admin/accounts',
    label: 'Учетные записи',
    description: 'Управление статусом пользователей и лимитом устройств.'
  }
];

export function requestStatusLabel(status: string): string {
  switch (status) {
    case 'pending':
      return 'Ожидает';
    case 'approved':
      return 'Одобрена';
    case 'rejected':
      return 'Отклонена';
    default:
      return status;
  }
}

export function userStatusLabel(active: boolean): string {
  return active ? 'активен' : 'неактивен';
}

export function actorTypeLabel(actorType: string): string {
  switch (actorType) {
    case 'user':
      return 'пользователь';
    case 'superadmin':
      return 'суперадминистратор';
    case 'system':
      return 'система';
    default:
      return actorType;
  }
}

export function entityTypeLabel(entityType: string): string {
  switch (entityType) {
    case 'vpn_user':
      return 'пользователь VPN';
    case 'vpn_request':
      return 'заявка на VPN';
    case 'vpn_device_credential':
      return 'учетные данные устройства VPN';
    case 'ip_change_confirmation':
      return 'историческое подтверждение смены IP';
    case 'trusted_ip':
      return 'IP устройства';
    default:
      return entityType;
  }
}

export function auditActionLabel(action: string): string {
  switch (action) {
    case 'request_approved':
      return 'Заявка одобрена';
    case 'request_rejected':
      return 'Заявка отклонена';
    case 'user_activated':
      return 'Пользователь активирован';
    case 'user_deactivated':
      return 'Пользователь деактивирован';
    case 'device_credential_issued':
      return 'Выданы учетные данные устройства';
    case 'device_credential_rotated':
      return 'Учетные данные устройства изменены';
    case 'device_ip_bound':
      return 'IP привязан к устройству';
    case 'device_ip_unbound':
      return 'IP отвязан от устройства';
    case 'ip_confirmation_requested':
      return 'Запрошено подтверждение IP (старый flow)';
    case 'ip_confirmed':
      return 'IP-адрес подтвержден (старый flow)';
    case 'vpn_new_ip_blocked':
      return 'Смена IP заблокирована до отвязки';
    case 'account_activated':
      return 'Учетная запись активирована';
    default:
      return action;
  }
}
