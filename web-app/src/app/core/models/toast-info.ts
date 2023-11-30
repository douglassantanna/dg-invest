import { TemplateRef } from "@angular/core";

export interface ToastInfo {
  header: string;
  body: string;
  delay?: number;
  classname: string;
  textOrTpl: TemplateRef<any> | null;
}
