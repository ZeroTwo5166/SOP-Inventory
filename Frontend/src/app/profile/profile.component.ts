import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { UserService } from '../services/user.service';
import { User } from '../models/user';
import { Loan } from '../models/loan';
import { Item } from '../models/item';
import { NavbarComponent } from '../navbar/navbar.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Login } from '../models/login';
import { Request } from '../models/request';
import { RequestService } from '../services/request.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css'],
  imports: [NavbarComponent, CommonModule, FormsModule],
})
export class ProfileComponent implements OnInit {
  users: User[] = [];
  loans: Loan[] = [];
  instuctors: User[] = [];
  confirmPassword: string = '';
  passwordMismatch: boolean = false;
  weakPassword: boolean = false;
  showPasswordModal: boolean = false;

  // Image upload properties
  selectedImage: File | null = null;
  selectedImagePreview: string | null = null;
  imageUpdated: boolean = false;
  isUploadingImage: boolean = false;
  uploadError: boolean = false;



  item: Item = {
    id: 0,
    roomId: 0,
    itemGroupId: 0,
    serialNumber: '',
    itemGroup: {
      id: 0,
      itemTypeId: 0,
      modelName: '',
      quantity: 0,
      price: 0,
      manufacturer: '',
      warrantyPeriod: '',
      itemType: {
        id: 0,
        typeName: '',
      },
    },
  };

  loan: Loan = {
    id: 0,
    loanDate: new Date('0000-00-00'),
    returnDate: new Date('0000-00-00'),
    itemId: 0,
    userId: 0,
  };

  user: User = {
    id: 0,
    name: '',
    email: '',
    password: '',
    twoFactorAuthentication: false,
    roleId: 0,
    role: {
      id: 0,
      name: '',
      description: '',
    },
    userLoans: [],
  };

  localCurrentUserLogin: Login = {
    id: 0,
    email: '',
    password: '',
    twoFactorAuthentication: false,
    token: '',
    role: {
      id: 0,
      name: '',
      description: '',
    },
  };

  instuctor: User = {
    id: 0,
    name: '',
    email: '',
    password: '',
    twoFactorAuthentication: false,
    roleId: 0,
    role: {
      id: 0,
      name: '',
      description: '',
    },
  };

  request: Request = {
    id: 0,
    userId: 0,
    recipientEmail: '',
    item: '',
    message: '',
    date: new Date(),
    status: '',
  };

  showRequestModal: boolean = false;
  remainingTime: number = 0;

  constructor(
    private router: Router,
    private authService: AuthService,
    private userService: UserService,
    private requestService: RequestService
  ) { }

  // Handle errors
  private handleError(error: any): void {
    switch (error.status) {
      case 0:
        alert('Ingen forbindelse til server. Prøv igen senere.');
        break;
      case 400:
        alert('Fejl i forespørgsel til serveren. Prøv igen senere.');
        break;
      case 500:
        alert('Fejl på Serveren. Prøv igen senere.');
        break;
      default:
        alert('En ukendt fejl opstod. Prøv igen senere.');
        break;
    }
  }

  async ngOnInit(): Promise<void> {
    this.remainingTime = this.authService.getRemainingTokenTime();
    console.log('Remaining token time (ms):', this.remainingTime);
    await this.getUser();
    this.getInstuctor();
  }

  // Image upload methods
  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedImage = input.files[0];

      const reader = new FileReader();
      reader.onload = () => {
        this.selectedImagePreview = reader.result as string;
      };
      reader.readAsDataURL(this.selectedImage);

      // Reset upload states
      this.imageUpdated = false;
      this.uploadError = false;
    }
  }
  

  resetImage(): void {
    this.selectedImagePreview = null;
    this.user.profileImageUrl = 'DELETE_IMAGE';
    (document.getElementById('profileImage') as HTMLInputElement).value = '';
    this.imageUpdated = true;
    this.selectedImage = null;
  }

  uploadImageToCloudinaryAsync(file: File): Promise<string> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('upload_preset', 'SOP_ProfileImages');

    const cloudName = 'dkrcapzct';
    const uploadUrl = `https://api.cloudinary.com/v1_1/${cloudName}/image/upload`;

    return fetch(uploadUrl, {
      method: 'POST',
      body: formData,
    })
      .then(res => {
        if (!res.ok) {
          throw new Error(`HTTP error! status: ${res.status}`);
        }
        return res.json();
      })
      .then(data => {
        return data.secure_url;
      });
  }

  // Method to get the user. \\
  async getUser(): Promise<void> {
    this.authService.currentUser.subscribe((x) => {
      if (x && x.id !== 0) {
        this.localCurrentUserLogin = x;
        this.userService.findById(this.localCurrentUserLogin.id).subscribe(
          (data) => {
            // Ensure that role is properly initialized if missing
            console.log("PROFILE COMPONENT");
            console.log(data);
            this.user = data;
            this.selectUser(data);
          },
          (error) => {
            console.error('Error fetching users', error);
            this.handleError(error);
          }
        );
      }
    });
  }

  // Method to get the instructor.
  getInstuctor(): void {
    this.userService.getUseresByRoleId(4).subscribe(
      (drift) => {
        this.instuctors = drift; // Add the first set of users
        this.userService.getUseresByRoleId(2).subscribe(
          (instructor) => {
            this.instuctors = [...this.instuctors, ...instructor]; // Append the second set of users
          },
          (error) => {
            console.error('Error fetching users', error);
            this.handleError(error);
          }
        );
      },
      (error) => {
        console.error('Error fetching users', error);
        this.handleError(error);
      }
    );
  }

  // Method to select the user. \\
  selectUser(user: User): void {
    this.user = { ...user };
    this.user.password = ''; // Clear the password field
    this.confirmPassword = ''; // Clear the confirm password field
    this.checkPasswords(); // Check passwords initially
  }

  // Check if passwords match and meet strength requirements
  checkPasswords() {
    const password = this.user.password;
    const confirmPassword = this.confirmPassword;

    // Password match check
    this.passwordMismatch = password !== confirmPassword;

    // Password strength check
    const hasUpperCase = /[A-Z]/.test(password);
    const hasLowerCase = /[a-z]/.test(password);
    const hasNumber = /[0-9]/.test(password);
    const hasSpecialChar = /[!@#$%^&*(),-.?":{}|<>]/.test(password);
    const hasMinLength = password?.length >= 15;

    this.weakPassword = !(
      hasUpperCase &&
      hasLowerCase &&
      hasNumber &&
      hasSpecialChar &&
      hasMinLength
    );
  }


  async saveChanges(): Promise<void> {
    if (this.isUploadingImage) {
      alert('Vent venligst på at billedet bliver uploadet...');
      return;
    }

    if (this.uploadError) {
      alert('Der opstod en fejl ved upload af billede. Prøv igen.');
      return;
    }

    if (!this.user) {
      console.log('No user to update');
      return;
    }

    // If user selected a new image but hasn't uploaded yet
    if (this.selectedImage && !this.imageUpdated) {
      try {
        this.isUploadingImage = true;
        const imageUrl = await this.uploadImageToCloudinaryAsync(this.selectedImage);
        this.user.profileImageUrl = imageUrl;
        this.imageUpdated = true;
        this.isUploadingImage = false;
      } catch (error) {
        this.uploadError = true;
        this.isUploadingImage = false;
        alert('Upload til Cloudinary mislykkedes.');
        return; // Stop saving if upload failed
      }
    }

    this.updateUser(this.user);
  }


  // Method to update the user. \\
  updateUser(user: User): void {
    user.roleId = Number(user.roleId);
    this.userService.update(user).subscribe(
      (updatedUser) => {
        this.handleSuccessfulUpdate(updatedUser);
      },
      (error) => {
        console.error('Error updating user without password', error);
        this.handleError(error);
      }
    );
    
  }

  savePassword(): void {
    if (this.passwordMismatch) {
      alert('Passwords do not match.');
      return;
    }

    if (this.weakPassword) {
      alert(
        'Password must be at least 15 characters and include uppercase, lowercase, numbers, and special characters.'
      );
      return;
    }

    // Update the user password
    if (this.user.password) {
      this.userService.updatePassword(this.user).subscribe(
        (updatedUser) => {
          alert('Password updated successfully!');
          this.user.password = '';
          this.confirmPassword = '';
          this.showPasswordModal = false;
        },
        (error) => {
          console.error('Error updating password', error);
          this.handleError(error);
        }
      );
    }
  }


  // Helper method to handle successful updates
  private handleSuccessfulUpdate(updatedUser: User): void {
    const index = this.users.findIndex((u) => u.id === updatedUser.id);
    if (index !== -1) {
      this.users[index] = updatedUser;
    }
    this.getUser();
    this.imageUpdated = false;
    this.selectedImage = null;
    this.selectedImagePreview = null;
    alert('Profil opdateret!');
  }


  // Method for navigation to item details. \\
  navigateToItemDetails(itemId: number | undefined): void {
    if (itemId !== undefined) {
      if (this.user.roleId === 3) {
        return;
      } else {
        this.router.navigate(['/itemDetails/', itemId]);
      }
    }
  }

  // Method for opening the request modal. \\
  openRequestModal(): void {
    console.log(this.instuctors);
    this.showRequestModal = true;
  }

  // Method for closing the request modal. \\
  closeEditModal(): void {
    this.request = {
      id: 0,
      userId: 0,
      recipientEmail: '',
      item: '',
      message: '',
      date: new Date(),
      status: '',
    };
    this.showRequestModal = false;
  }

  async bindRequest(): Promise<void> {
    this.request.userId = this.user.id;
    this.request.date = new Date();
    this.request.status = 'Sent';
    console.log('Adding request:', this.request);
  }

  // Method for creating a request. \\
  async createRequest(): Promise<void> {
    await this.bindRequest();
    this.requestService.create(this.request).subscribe(
      (response) => {
        console.log('Request created successfully:', response);
        console.log(this.request);
        this.closeEditModal();
      },
      (error) => {
        console.error('Error adding request', error);
        this.handleError(error);
      }
    );
  }
}
