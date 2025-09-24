import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  standalone: true,
  imports: [FormsModule, CommonModule],
})
export class LoginComponent implements OnInit {
  email: string = '';
  password: string = '';
  role: string = '';
  qrCodeUrl: string = '';
  otp: string = '';
  showOtpSection: boolean = false;
  showOtpVerificationSection: boolean = false;
  
  loginError: string = '';
  otpError: string = '';
  otpVerified : boolean = false;

  constructor(private router: Router, private authService: AuthService) { }


  ngOnInit() {

    const currentUser = this.authService.currentUserValue;

    // if (currentUser) {
    //   this.router.navigate(['/home']);
    // }

    // if(this.otpVerified){
    //   this.router.navigate(['/home']);
    // }

  }

  login() {
      this.loginError = ''; // 👈 Clear previous error

    this.authService.login(this.email, this.password, this.role).subscribe({
      next: () => {

        this.authService.getQrCode(this.email).subscribe({
          next: (res) => {
            if(res?.qrCodeImage){
              this.qrCodeUrl = res.qrCodeImage;
              this.showOtpSection = true;
              console.log("IF")

            } else {
              this.showOtpSection = false;
              this.showOtpVerificationSection = true;
              console.log("ELSE")

            }

          },
          error: (err) => {
            console.error('QR code fetch failed:', err);
            this.loginError = 'Kunne ikke hente QR-kode. Prøv igen senere.';
          }
        });
      },
      error: (error) => this.handleLoginError(error)
    });
  }

  resetLogin() {
  this.email = '';
  this.password = '';
  this.otp = '';
  this.qrCodeUrl = '';
  this.loginError = '';
  this.otpError = '';
  this.showOtpSection = false;
  this.showOtpVerificationSection = false;

}


  verifyOtp() {

    this.otpError = ''; 

    this.authService.verifyOtp(this.email, this.otp).subscribe({
      next: (res) => {
        console.log(res);
        this.otpVerified = true;
        this.router.navigate(['/home']); // ✅ Navigate after successful OTP

      },
      error: (err) => {
        console.error('OTP verification failed:', err);
        this.otpError = 'Ugyldigt engangskode. Prøv igen.';
      }
    });
  }

  private handleLoginError(error: any): void {
      console.error('Login error object:', error); // 👈 Add this line for debugging

    switch (error.status) {
      case 0:
        this.loginError = 'Ingen forbindelse til server. Prøv igen senere.';
        break;
      case 401:
        this.loginError = 'Email eller kode er forkert, prøv igen.';
        break;
      case 500:
        this.loginError = 'Fejl på serveren. Prøv igen senere.';
        break;
      default:
        this.loginError = 'En ukendt fejl opstod. Prøv igen senere.';
        break;
    }
  }


}