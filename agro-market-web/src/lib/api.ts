import type {
  AdminUserItem,
  AuthResponse,
  CheckoutGroup,
  FulfillmentType,
  Order,
  PagedResult,
  PaymentMethod,
  PresignedUploadResult,
  Product,
  ProductMetadata,
  ProfileResponse,
  SendOtpResponse,
  TelegramDeepLinkResponse,
  UserRole,
  UserSummary,
} from "./types";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL?.replace(/\/$/, "") || "http://localhost:5095";

const ACCESS_TOKEN_KEY = "agromarket_access_token";
const REFRESH_TOKEN_KEY = "agromarket_refresh_token";

export class ApiError extends Error {
  status: number;
  details?: Record<string, string[]>;

  constructor(message: string, status: number, details?: Record<string, string[]>) {
    super(message);
    this.status = status;
    this.details = details;
  }
}

export function getStoredTokens() {
  if (typeof window === "undefined") return { accessToken: null, refreshToken: null };
  return {
    accessToken: localStorage.getItem(ACCESS_TOKEN_KEY),
    refreshToken: localStorage.getItem(REFRESH_TOKEN_KEY),
  };
}

export function setStoredTokens(accessToken: string, refreshToken: string) {
  if (typeof window === "undefined") return;
  localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
  localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
}

export function clearStoredTokens() {
  if (typeof window === "undefined") return;
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
}

async function parseError(res: Response): Promise<ApiError> {
  try {
    const body = await res.json();
    const message =
      body.detail || body.title || `Request failed with status ${res.status}`;
    return new ApiError(message, res.status, body.errors);
  } catch {
    return new ApiError(`Request failed with status ${res.status}`, res.status);
  }
}

async function tryRefreshTokens(): Promise<string | null> {
  const { refreshToken } = getStoredTokens();
  if (!refreshToken) return null;

  const res = await fetch(`${API_BASE}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });

  if (!res.ok) {
    clearStoredTokens();
    return null;
  }

  const data = (await res.json()) as AuthResponse;
  setStoredTokens(data.accessToken, data.refreshToken);
  return data.accessToken;
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit & { auth?: boolean } = {}
): Promise<T> {
  const { auth = false, headers, ...rest } = options;
  const { accessToken } = getStoredTokens();

  const buildHeaders = (token: string | null) => ({
    ...(rest.body ? { "Content-Type": "application/json" } : {}),
    ...(auth && token ? { Authorization: `Bearer ${token}` } : {}),
    ...headers,
  });

  let res = await fetch(`${API_BASE}${path}`, {
    ...rest,
    headers: buildHeaders(accessToken),
  });

  if (res.status === 401 && auth) {
    const refreshed = await tryRefreshTokens();
    if (refreshed) {
      res = await fetch(`${API_BASE}${path}`, {
        ...rest,
        headers: buildHeaders(refreshed),
      });
    }
  }

  if (!res.ok) {
    throw await parseError(res);
  }

  if (res.status === 204) {
    return undefined as T;
  }

  return (await res.json()) as T;
}

export const authApi = {
  sendOtp: (phoneNumber: string) =>
    apiFetch<SendOtpResponse>("/api/auth/send-otp", {
      method: "POST",
      body: JSON.stringify({ phoneNumber }),
    }),

  verifyOtp: (payload: {
    phoneNumber: string;
    code: string;
    fullName?: string;
    role?: "Farmer" | "Restaurant";
  }) =>
    apiFetch<AuthResponse>("/api/auth/verify-otp", {
      method: "POST",
      body: JSON.stringify(payload),
    }),

  me: () => apiFetch<UserSummary>("/api/auth/me", { auth: true }),

  telegramDeepLink: () =>
    apiFetch<TelegramDeepLinkResponse>("/api/auth/telegram-deep-link", {
      auth: true,
    }),

  logout: async () => {
    const { refreshToken } = getStoredTokens();
    if (refreshToken) {
      await apiFetch<void>("/api/auth/logout", {
        method: "POST",
        body: JSON.stringify({ refreshToken }),
      }).catch(() => undefined);
    }
    clearStoredTokens();
  },
};

export const profileApi = {
  getMine: () => apiFetch<ProfileResponse>("/api/profiles/me", { auth: true }),

  upsertMine: (payload: {
    fullName?: string;
    businessName: string;
    region: string;
    address: string;
    preferredDeliveryWindow?: string;
    taxId?: string;
  }) =>
    apiFetch<ProfileResponse>("/api/profiles/me", {
      method: "PUT",
      auth: true,
      body: JSON.stringify(payload),
    }),
};

export const adminApi = {
  listUsers: (params?: {
    role?: UserRole | "";
    isVerified?: boolean | "";
    search?: string;
  }) => {
    const qs = new URLSearchParams();
    if (params?.role) qs.set("role", params.role);
    if (params?.isVerified !== "" && params?.isVerified !== undefined) {
      qs.set("isVerified", String(params.isVerified));
    }
    if (params?.search) qs.set("search", params.search);
    const suffix = qs.toString() ? `?${qs.toString()}` : "";
    return apiFetch<PagedResult<AdminUserItem>>(`/api/admin/users${suffix}`, {
      auth: true,
    });
  },

  verifyUser: (id: string, isVerified: boolean, role?: UserRole) =>
    apiFetch<AdminUserItem>(`/api/admin/users/${id}/verify`, {
      method: "PATCH",
      auth: true,
      body: JSON.stringify({ isVerified, ...(role ? { role } : {}) }),
    }),
};

export const productsApi = {
  listCatalog: (params?: {
    category?: string;
    region?: string;
    search?: string;
    isOrganic?: boolean;
  }) => {
    const qs = new URLSearchParams();
    if (params?.category) qs.set("category", params.category);
    if (params?.region) qs.set("region", params.region);
    if (params?.search) qs.set("search", params.search);
    if (params?.isOrganic) qs.set("isOrganic", "true");
    const suffix = qs.toString() ? `?${qs.toString()}` : "";
    return apiFetch<PagedResult<Product>>(`/api/products${suffix}`);
  },

  listMine: () =>
    apiFetch<PagedResult<Product>>("/api/products/mine", { auth: true }),

  createUploadUrl: (fileName: string, contentType: string) =>
    apiFetch<PresignedUploadResult>("/api/products/upload-url", {
      method: "POST",
      auth: true,
      body: JSON.stringify({ fileName, contentType }),
    }),

  create: (payload: {
    category: string;
    title: string;
    description?: string;
    availableStockKg: number;
    minOrderKg: number;
    basePricePerKg: number;
    metadata?: Partial<ProductMetadata>;
    imageUrls?: string[];
    priceTiers?: { minQuantityKg: number; pricePerKg: number }[];
  }) =>
    apiFetch<Product>("/api/products", {
      method: "POST",
      auth: true,
      body: JSON.stringify(payload),
    }),

  update: (
    id: string,
    payload: {
      availableStockKg?: number;
      basePricePerKg?: number;
      isActive?: boolean;
      rowVersion?: number;
    }
  ) =>
    apiFetch<Product>(`/api/products/${id}`, {
      method: "PATCH",
      auth: true,
      body: JSON.stringify(payload),
    }),

  remove: (id: string) =>
    apiFetch<void>(`/api/products/${id}`, {
      method: "DELETE",
      auth: true,
    }),
};

export const ordersApi = {
  checkout: (payload: {
    items: { productId: string; weightKg: number }[];
    deliveryAddress?: string;
    paymentMethod?: PaymentMethod;
    fulfillmentType?: FulfillmentType;
  }) =>
    apiFetch<CheckoutGroup>("/api/orders/checkout", {
      method: "POST",
      auth: true,
      body: JSON.stringify(payload),
    }),

  repeatOrder: (checkoutGroupId: string) =>
    apiFetch<CheckoutGroup>(`/api/orders/${checkoutGroupId}/repeat`, {
      method: "POST",
      auth: true,
      body: JSON.stringify({}),
    }),

  listCheckoutGroups: () =>
    apiFetch<PagedResult<CheckoutGroup>>("/api/orders/checkout-groups", {
      auth: true,
    }),

  listOrders: () =>
    apiFetch<PagedResult<Order>>("/api/orders", { auth: true }),
};
