import { NZ_ICONS, NzIconModule } from 'ng-zorro-antd/icon';
import { IconDefinition } from '@ant-design/icons-angular';
import {
  MenuFoldOutline,
  MenuUnfoldOutline,
  FormOutline,
  DashboardOutline
} from '@ant-design/icons-angular/icons';

const icons: IconDefinition[] = [MenuFoldOutline, MenuUnfoldOutline, FormOutline, DashboardOutline];

export const provideNzIcons = () => [
  { provide: NZ_ICONS, useValue: icons }
];
