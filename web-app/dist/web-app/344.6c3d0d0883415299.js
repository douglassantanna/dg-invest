"use strict";(self.webpackChunkweb_app=self.webpackChunkweb_app||[]).push([[344],{6344:(_,d,o)=>{o.r(d),o.d(d,{UserProfileComponent:()=>k});var g=o(177),e=o(4438),n=o(4341),p=o(4893),h=o(5423),c=o(3443),f=o(3587);function b(t,u){1&t&&e.nrm(0,"span",19)}function m(t,u){1&t&&e.nrm(0,"span",19)}function w(t,u){if(1&t){const a=e.RV6();e.j41(0,"div",27)(1,"h2",12),e.EFF(2,"Preferences"),e.k0s(),e.j41(3,"div",13)(4,"div",28)(5,"span",29),e.EFF(6,"Dark Mode"),e.k0s(),e.j41(7,"button",30),e.bIt("click",function(){e.eBV(a);const r=e.XpG();return e.Njj(r.toggleDarkMode())}),e.j41(8,"span",31),e.EFF(9,"Toggle dark mode"),e.k0s(),e.nrm(10,"span",32),e.k0s()(),e.j41(11,"div",28)(12,"span",29),e.EFF(13,"Email Notifications"),e.k0s(),e.j41(14,"button",33)(15,"span",31),e.EFF(16,"Enable notifications"),e.k0s(),e.nrm(17,"span",34),e.k0s()()()()}if(2&t){const a=e.XpG();e.R7$(10),e.HbH(a.isDarkMode?"translate-x-5":"translate-x-0")}}let k=(()=>{class t{constructor(){this.isDarkMode=!1,this.authService=(0,e.WQX)(p.u),this.userService=(0,e.WQX)(c.D),this.toastService=(0,e.WQX)(h.f),this.btnColor=f.c.btnColor,this.loading=!1,this.userFullname="",this.userEmail="",this.updatePasswordModel=(0,e.vPA)({userId:0,currentPassword:"",newPassword:"",confirmNewPassword:""}),this.hasUpperCase=(0,e.vPA)(!1),this.hasLowerCase=(0,e.vPA)(!1),this.hasNumbers=(0,e.vPA)(!1),this.hasSpecialChars=(0,e.vPA)(!1),this.hasSixChars=(0,e.vPA)(!1)}toggleDarkMode(){}ngOnInit(){this.authService.user&&(this.userFullname=this.authService.user.unique_name,this.userEmail=this.authService.user.email)}getUserInitial(){return this.userFullname.charAt(0).toUpperCase()}updateUserProfile(){(this.userEmail!==this.authService.user?.email||this.userFullname!==this.authService.user.unique_name)&&(this.loading=!0,this.userService.updateUserProfile(this.userFullname,this.userEmail,this.authService.user?.nameid).subscribe({next:()=>{this.loading=!1,this.toastService.showSuccess("Your profile was updated successfully. Please, log in again."),this.authService.logout()},error:()=>{this.loading=!1,this.toastService.showError("There was an error updating your profile.")}}))}updatePassword(){this.updatePasswordModel().newPassword&&this.updatePasswordModel().currentPassword?(this.loading=!1,this.updatePasswordModel().userId=Number(this.authService.user?.nameid),this.userService.updateUserPassword(this.updatePasswordModel()).subscribe({next:()=>{this.loading=!1,this.toastService.showSuccess("Your password was updated successfully. Please, log in again."),this.authService.logout()},error:a=>{a.error.data.validationErrors.forEach(r=>{this.toastService.showError(r)}),this.loading=!1}})):this.toastService.showError("Please, fill in the password and current password fields")}arePasswordChecksTrue(){return this.hasLowerCase()&&this.hasNumbers()&&this.hasSixChars()&&this.hasSpecialChars()&&this.hasUpperCase()}passwordChecker(){const a=this.updatePasswordModel().newPassword;this.hasSixChars.set(a.length>=6),this.hasUpperCase.set(/[A-Z]/.test(a)),this.hasLowerCase.set(/[a-z]/.test(a)),this.hasNumbers.set(/\d/.test(a)),this.hasSpecialChars.set(/[!@#$%^&*(),.?":{}|<>]/.test(a))}static{this.\u0275fac=function(i){return new(i||t)}}static{this.\u0275cmp=e.VBU({type:t,selectors:[["app-user-profile"]],standalone:!0,features:[e.aNF],decls:71,vars:27,consts:[[1,"min-h-screen","bg-gray-50","dark:bg-gray-900"],[1,"bg-white","dark:bg-gray-800","shadow"],[1,"max-w-7xl","mx-auto","p-6"],[1,"flex","items-center","space-x-8"],[1,"h-16","w-16","rounded-full","bg-blue-100","dark:bg-blue-900","flex","items-center","justify-center"],[1,"text-4xl","font-bold","text-blue-600","dark:text-blue-300"],[1,"text-lg","font-bold","text-gray-900","dark:text-gray-100"],[1,"text-sm","text-gray-500","dark:text-gray-400"],[1,"max-w-7xl","mx-auto","py-6","px-4","sm:px-6","lg:px-8"],[1,"grid","grid-cols-1","lg:grid-cols-3","gap-6"],["id","personal-info",1,"lg:col-span-2","space-y-6"],[1,"bg-white","dark:bg-gray-800","shadow","rounded-lg","p-6"],[1,"text-lg","font-medium","text-gray-900","dark:text-gray-100","mb-6"],[1,"space-y-4"],[1,"block","text-sm","font-medium","text-gray-700","dark:text-gray-300"],["type","text","name","fullName",1,"w-full","px-4","py-2","text-sm","text-gray-900","bg-gray-50","border","border-gray-300","rounded-lg","focus:ring-blue-500","focus:border-blue-500","dark:bg-gray-700","dark:border-gray-600","dark:placeholder-gray-400","dark:text-white","dark:focus:ring-blue-500","dark:focus:border-blue-500",3,"ngModelChange","ngModel"],["type","email","name","email",1,"w-full","px-4","py-2","text-sm","text-gray-900","bg-gray-50","border","border-gray-300","rounded-lg","focus:ring-blue-500","focus:border-blue-500","dark:bg-gray-700","dark:border-gray-600","dark:placeholder-gray-400","dark:text-white","dark:focus:ring-blue-500","dark:focus:border-blue-500",3,"ngModelChange","ngModel"],[1,"flex","justify-end"],["type","button",1,"inline-flex","items-center","px-4","py-2","border","border-transparent","rounded-md","shadow-sm","text-sm","font-medium","text-white","bg-blue-600","hover:bg-blue-700","focus:outline-none","focus:ring-2","focus:ring-offset-2","focus:ring-blue-500","disabled:opacity-50","disabled:cursor-not-allowed",3,"click"],[1,"ml-2","inline-block","h-4","w-4","animate-spin","rounded-full","border-2","border-white","border-t-transparent"],["id","security",1,"bg-white","dark:bg-gray-800","shadow","rounded-lg","p-6"],["type","password","name","currentPassword",1,"w-full","px-4","py-2","text-sm","text-gray-900","bg-gray-50","border","border-gray-300","rounded-lg","focus:ring-blue-500","focus:border-blue-500","dark:bg-gray-700","dark:border-gray-600","dark:placeholder-gray-400","dark:text-white","dark:focus:ring-blue-500","dark:focus:border-blue-500",3,"ngModelChange","ngModel"],["type","password","name","newPassword",1,"w-full","px-4","py-2","text-sm","text-gray-900","bg-gray-50","border","border-gray-300","rounded-lg","focus:ring-blue-500","focus:border-blue-500","dark:bg-gray-700","dark:border-gray-600","dark:placeholder-gray-400","dark:text-white","dark:focus:ring-blue-500","dark:focus:border-blue-500",3,"ngModelChange","input","ngModel"],["id","password-requirements",1,"mt-3","grid","grid-cols-2","gap-2","text-sm"],[1,"flex","items-center","space-x-2"],[1,"text-gray-600","dark:text-gray-400"],["type","button",1,"inline-flex","items-center","px-4","py-2","border","border-transparent","rounded-md","shadow-sm","text-sm","font-medium","text-white","bg-blue-600","hover:bg-blue-700","focus:outline-none","focus:ring-2","focus:ring-offset-2","focus:ring-blue-500","disabled:opacity-50","disabled:cursor-not-allowed",3,"click","disabled"],["id","preferences",1,"bg-white","dark:bg-gray-800","shadow","rounded-lg","p-6","h-fit"],[1,"flex","items-center","justify-between"],[1,"text-sm","font-medium","text-gray-700","dark:text-gray-300"],["type","button",1,"relative","inline-flex","h-6","w-11","flex-shrink-0","cursor-pointer","rounded-full","border-2","border-transparent","transition-colors","duration-200","ease-in-out","focus:outline-none","focus:ring-2","focus:ring-blue-500","focus:ring-offset-2","bg-gray-200","dark:bg-blue-600",3,"click"],[1,"sr-only"],[1,"pointer-events-none","relative","inline-block","h-5","w-5","transform","rounded-full","bg-white","shadow","ring-0","transition","duration-200","ease-in-out"],["type","button",1,"relative","inline-flex","h-6","w-11","flex-shrink-0","cursor-pointer","rounded-full","border-2","border-transparent","transition-colors","duration-200","ease-in-out","focus:outline-none","focus:ring-2","focus:ring-blue-500","focus:ring-offset-2","bg-gray-200"],[1,"translate-x-0","pointer-events-none","relative","inline-block","h-5","w-5","transform","rounded-full","bg-white","shadow","ring-0","transition","duration-200","ease-in-out"]],template:function(i,r){1&i&&(e.j41(0,"div",0)(1,"div",1)(2,"div",2)(3,"div",3)(4,"div",4)(5,"span",5),e.EFF(6),e.nI1(7,"slice"),e.k0s()(),e.j41(8,"div")(9,"h1",6),e.EFF(10),e.k0s(),e.j41(11,"p",7),e.EFF(12),e.k0s()()()()(),e.j41(13,"div",8)(14,"div",9)(15,"form")(16,"div",10)(17,"div",11)(18,"h2",12),e.EFF(19,"Personal Information"),e.k0s(),e.j41(20,"div",13)(21,"div")(22,"label",14),e.EFF(23,"Full Name"),e.k0s(),e.j41(24,"input",15),e.mxI("ngModelChange",function(s){return e.DH7(r.userFullname,s)||(r.userFullname=s),s}),e.k0s()(),e.j41(25,"div")(26,"label",14),e.EFF(27,"Email"),e.k0s(),e.j41(28,"input",16),e.mxI("ngModelChange",function(s){return e.DH7(r.userEmail,s)||(r.userEmail=s),s}),e.k0s()(),e.j41(29,"div",17)(30,"button",18),e.bIt("click",function(){return r.updateUserProfile()}),e.EFF(31," Save Changes "),e.DNE(32,b,1,0,"span",19),e.k0s()()()(),e.j41(33,"div",20)(34,"h2",12),e.EFF(35,"Security"),e.k0s(),e.j41(36,"div",13)(37,"div")(38,"label",14),e.EFF(39,"Current Password"),e.k0s(),e.j41(40,"input",21),e.mxI("ngModelChange",function(s){return e.DH7(r.updatePasswordModel().currentPassword,s)||(r.updatePasswordModel().currentPassword=s),s}),e.k0s()(),e.j41(41,"div")(42,"label",14),e.EFF(43,"New Password"),e.k0s(),e.j41(44,"input",22),e.mxI("ngModelChange",function(s){return e.DH7(r.updatePasswordModel().newPassword,s)||(r.updatePasswordModel().newPassword=s),s}),e.bIt("input",function(){return r.passwordChecker()}),e.k0s(),e.j41(45,"div",23)(46,"div",24)(47,"span"),e.EFF(48),e.k0s(),e.j41(49,"span",25),e.EFF(50,"Lowercase"),e.k0s()(),e.j41(51,"div",24)(52,"span"),e.EFF(53),e.k0s(),e.j41(54,"span",25),e.EFF(55,"Uppercase"),e.k0s()(),e.j41(56,"div",24)(57,"span"),e.EFF(58),e.k0s(),e.j41(59,"span",25),e.EFF(60,"Numbers"),e.k0s()(),e.j41(61,"div",24)(62,"span"),e.EFF(63),e.k0s(),e.j41(64,"span",25),e.EFF(65,"Special Chars"),e.k0s()()()(),e.j41(66,"div",17)(67,"button",26),e.bIt("click",function(){return r.updatePassword()}),e.EFF(68," Update Password "),e.DNE(69,m,1,0,"span",19),e.k0s()()()()()(),e.DNE(70,w,18,2,"div",27),e.k0s()()()),2&i&&(e.R7$(6),e.SpI(" ",e.brH(7,23,r.userFullname,0,1)," "),e.R7$(4),e.JRh(r.userFullname),e.R7$(2),e.JRh(r.userEmail),e.R7$(12),e.R50("ngModel",r.userFullname),e.R7$(4),e.R50("ngModel",r.userEmail),e.R7$(4),e.vxM(32,r.loading?32:-1),e.R7$(8),e.R50("ngModel",r.updatePasswordModel().currentPassword),e.R7$(4),e.R50("ngModel",r.updatePasswordModel().newPassword),e.R7$(3),e.HbH(r.hasLowerCase()?"text-green-500":"text-gray-400"),e.R7$(),e.SpI(" ",r.hasLowerCase()?"\u2713":"\u25cb"," "),e.R7$(4),e.HbH(r.hasUpperCase()?"text-green-500":"text-gray-400"),e.R7$(),e.SpI(" ",r.hasUpperCase()?"\u2713":"\u25cb"," "),e.R7$(4),e.HbH(r.hasNumbers()?"text-green-500":"text-gray-400"),e.R7$(),e.SpI(" ",r.hasNumbers()?"\u2713":"\u25cb"," "),e.R7$(4),e.HbH(r.hasSpecialChars()?"text-green-500":"text-gray-400"),e.R7$(),e.SpI(" ",r.hasSpecialChars()?"\u2713":"\u25cb"," "),e.R7$(4),e.Y8G("disabled",!r.arePasswordChecksTrue()||r.loading),e.R7$(2),e.vxM(69,r.loading?69:-1),e.R7$(),e.vxM(70,-1))},dependencies:[g.P9,n.YN,n.qT,n.me,n.BC,n.cb,n.vS,n.cV],encapsulation:2})}}return t})()}}]);