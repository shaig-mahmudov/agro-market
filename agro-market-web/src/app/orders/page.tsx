"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { adminApi, ApiError, ordersApi } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import type { CheckoutGroup, Order, PaymentMethod, PaymentStatus } from "@/lib/types";

export default function OrdersPage() {
  const { user } = useAuth();
  const [groups, setGroups] = useState<CheckoutGroup[]>([]);
  const [farmerOrders, setFarmerOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [repeatingId, setRepeatingId] = useState<string | null>(null);
  const [respondingId, setRespondingId] = useState<string | null>(null);
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [payingOrderId, setPayingOrderId] = useState<string | null>(null);

  const reload = useCallback(async () => {
    if (!user) return;
    setLoading(true);
    setError(null);
    try {
      if (user.role === "Farmer") {
        const res = await ordersApi.listOrders();
        setFarmerOrders(res.items);
      } else {
        const res = await ordersApi.listCheckoutGroups();
        setGroups(res.items);
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load orders.");
    } finally {
      setLoading(false);
    }
  }, [user]);

  useEffect(() => {
    if (!user) return;
    let active = true;
    const req =
      user.role === "Farmer"
        ? ordersApi.listOrders().then((res) => {
            if (active) setFarmerOrders(res.items);
          })
        : ordersApi.listCheckoutGroups().then((res) => {
            if (active) setGroups(res.items);
          });

    req
      .catch((err) => {
        if (active) {
          setError(err instanceof ApiError ? err.message : "Failed to load orders.");
        }
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [user]);

  if (!user) {
    return (
      <div className="mx-auto max-w-lg rounded-2xl border border-[#DCE6DF] bg-white p-6 text-center">
        <p className="font-semibold">Sign in to view your orders.</p>
        <Link
          href="/login"
          className="mt-4 inline-block rounded-xl bg-[#1B4332] px-4 py-2 text-sm font-semibold text-white"
        >
          Sign in with OTP
        </Link>
      </div>
    );
  }

  async function handleRepeat(checkoutGroupId: string) {
    setRepeatingId(checkoutGroupId);
    setError(null);
    try {
      await ordersApi.repeatOrder(checkoutGroupId);
      await reload();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not repeat order.");
    } finally {
      setRepeatingId(null);
    }
  }

  async function handleRespond(orderId: string, accept: boolean) {
    setRespondingId(orderId);
    setError(null);
    try {
      await ordersApi.respond(orderId, accept);
      await reload();
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : accept
            ? "Could not confirm order."
            : "Could not reject order."
      );
    } finally {
      setRespondingId(null);
    }
  }

  async function handleConfirmReceipt(orderId: string, approve: boolean) {
    setConfirmingId(orderId);
    setError(null);
    try {
      await ordersApi.confirmReceipt(orderId, approve);
      await reload();
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : approve
            ? "Could not approve delivery weight."
            : "Could not dispute delivery."
      );
    } finally {
      setConfirmingId(null);
    }
  }

  async function handleSetPaymentStatus(
    orderId: string,
    paymentStatus: PaymentStatus,
    paymentMethod?: PaymentMethod
  ) {
    setPayingOrderId(orderId);
    setError(null);
    try {
      await adminApi.markOrderPaid(orderId, paymentStatus, paymentMethod);
      await reload();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not update payment status.");
    } finally {
      setPayingOrderId(null);
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-[#1B4332]">
            {user.role === "Farmer" ? "Incoming Farm Orders" : "Consolidated Orders"}
          </h1>
          <p className="text-sm text-stone-500">
            {user.role === "Farmer"
              ? "Orders split from morning restaurant checkouts."
              : "Multi-farmer checkouts grouped into one next-morning delivery with 1-click repeat ordering."}
          </p>
        </div>
        <Link
          href="/"
          className="rounded-xl border border-[#D4DFD7] bg-white px-4 py-2 text-sm font-semibold text-[#1B4332]"
        >
          Browse Produce
        </Link>
      </div>

      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-800">
          {error}
        </div>
      )}

      {loading ? (
        <div className="rounded-2xl border border-[#E2E8E4] bg-white p-8 text-center text-sm text-stone-500">
          Loading orders...
        </div>
      ) : user.role === "Farmer" ? (
        farmerOrders.length === 0 ? (
          <div className="rounded-2xl border border-dashed border-[#C7D3CC] bg-white p-8 text-center text-sm text-stone-500">
            No incoming orders yet.
          </div>
        ) : (
          <div className="space-y-4">
            {farmerOrders.map((order) => (
              <div
                key={order.id}
                className="rounded-2xl border border-[#E2E8E4] bg-white p-5 shadow-xs"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <p className="font-bold text-[#1F2933]">
                      Buyer: {order.restaurantName ?? "Restaurant"}
                    </p>
                    <p className="text-xs text-stone-500">
                      Order #{order.id.slice(0, 8)} • Expires{" "}
                      {new Date(order.expiresAt).toLocaleString()}
                    </p>
                  </div>
                  <span className="rounded-full bg-[#E9F5EE] px-3 py-1 text-xs font-semibold text-[#1B4332]">
                    {order.status}
                  </span>
                </div>
                <div className="mt-3 divide-y divide-[#F0F3F1] text-sm">
                  {order.items.map((item) => (
                    <div key={item.id} className="flex justify-between py-1.5">
                      <span>
                        {item.productTitleSnapshot} ({item.estimatedWeightKg} kg × $
                        {item.unitPriceApplied.toFixed(2)})
                      </span>
                      <span className="font-semibold">
                        ${(item.estimatedWeightKg * item.unitPriceApplied).toFixed(2)}
                      </span>
                    </div>
                  ))}
                </div>
                {order.status === "Pending" && (
                  <div className="mt-4 flex flex-wrap items-center justify-end gap-2 border-t border-[#F0F3F1] pt-3">
                    <button
                      type="button"
                      disabled={respondingId === order.id}
                      onClick={() => void handleRespond(order.id, false)}
                      className="rounded-xl border border-red-200 bg-red-50 px-3.5 py-2 text-xs font-semibold text-red-700 hover:bg-red-100 disabled:opacity-50"
                    >
                      {respondingId === order.id ? "Updating..." : "Reject & Restore Stock"}
                    </button>
                    <button
                      type="button"
                      disabled={respondingId === order.id}
                      onClick={() => void handleRespond(order.id, true)}
                      className="rounded-xl bg-[#1B4332] px-4 py-2 text-xs font-semibold text-white hover:bg-[#2D6A4F] disabled:opacity-50"
                    >
                      {respondingId === order.id ? "Updating..." : "Accept Order"}
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        )
      ) : groups.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-[#C7D3CC] bg-white p-8 text-center text-sm text-stone-500">
          You have not placed any consolidated orders yet.
        </div>
      ) : (
        <div className="space-y-5">
          {groups.map((group) => {
            const grandTotal = group.orders
              .filter((o) => o.status !== "Cancelled")
              .reduce((acc, o) => acc + o.finalTotalAmount, 0)
              .toFixed(2);

            return (
              <section
                key={group.id}
                className="rounded-2xl border border-[#DCE6DF] bg-white p-5 shadow-xs"
              >
                <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[#F0F3F1] pb-4">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-wider text-[#2D6A4F]">
                      Scheduled Morning Delivery: {group.scheduledDeliveryDate}
                    </p>
                    <h2 className="text-lg font-bold text-[#1B4332]">
                      Checkout #{group.id.slice(0, 8)} — ${grandTotal}
                    </h2>
                    <p className="text-xs text-stone-500">
                      Deliver to: {group.deliveryAddress} • Delivery Fee: $
                      {group.totalDeliveryFee.toFixed(2)}
                    </p>
                  </div>

                  <button
                    type="button"
                    disabled={repeatingId === group.id}
                    onClick={() => void handleRepeat(group.id)}
                    className="rounded-xl bg-[#1B4332] px-4 py-2 text-xs font-semibold text-white hover:bg-[#2D6A4F] disabled:opacity-60"
                  >
                    {repeatingId === group.id ? "Repeating..." : "1-Click Repeat Order"}
                  </button>
                </div>

                <div className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-2">
                  {group.orders.map((order) => {
                    const needsApproval =
                      order.status === "DeliveredPendingApproval" || order.status === "Disputed";
                    const canBuyerConfirm =
                      needsApproval && (user.role === "Restaurant" || user.role === "Admin");
                    const isAdmin = user.role === "Admin";

                    return (
                      <div
                        key={order.id}
                        className="rounded-xl border border-[#E2E8E4] bg-[#FAF9F6] p-3.5 text-xs"
                      >
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <span className="font-bold text-[#1F2933]">
                            {order.farmerName ?? "Farm"}
                          </span>
                          <div className="flex items-center gap-1.5">
                            <span
                              className={`rounded-full px-2 py-0.5 font-semibold ${
                                order.paymentStatus === "Paid"
                                  ? "bg-emerald-100 text-emerald-800"
                                  : order.paymentStatus === "Disputed"
                                    ? "bg-red-100 text-red-800"
                                    : "bg-stone-200 text-stone-700"
                              }`}
                            >
                              {order.paymentMethod} • {order.paymentStatus}
                            </span>
                            <span
                              className={`rounded-full px-2.5 py-0.5 font-semibold ${
                                order.status === "DeliveredPendingApproval" ||
                                order.status === "Disputed"
                                  ? "bg-amber-100 text-amber-800"
                                  : "bg-[#D8F3DC] text-[#1B4332]"
                              }`}
                            >
                              {order.status}
                            </span>
                          </div>
                        </div>

                        <div className="mt-2 space-y-1">
                          {order.items.map((item) => {
                            const hasActual = item.actualDeliveredWeightKg !== null;
                            const chargeableKg = hasActual
                              ? Math.max(
                                  0,
                                  (item.actualDeliveredWeightKg ?? 0) -
                                    (item.rejectedWeightKg ?? 0)
                                )
                              : item.estimatedWeightKg;

                            return (
                              <div key={item.id} className="space-y-0.5 text-stone-700">
                                <div className="flex justify-between">
                                  <span>
                                    {item.productTitleSnapshot} (
                                    {hasActual
                                      ? `Actual ${item.actualDeliveredWeightKg} kg`
                                      : `Est ${item.estimatedWeightKg} kg`}
                                    {(item.rejectedWeightKg ?? 0) > 0
                                      ? `, Rej ${item.rejectedWeightKg} kg`
                                      : ""}{" "}
                                    @ ${item.unitPriceApplied.toFixed(2)}/kg)
                                  </span>
                                  <span className="font-semibold">
                                    ${(chargeableKg * item.unitPriceApplied).toFixed(2)}
                                  </span>
                                </div>
                                {item.proofPhotoUrl && (
                                  <a
                                    href={item.proofPhotoUrl}
                                    target="_blank"
                                    rel="noreferrer"
                                    className="text-[11px] font-medium text-[#2D6A4F] underline"
                                  >
                                    View delivery/scale proof
                                  </a>
                                )}
                              </div>
                            );
                          })}
                        </div>

                        <div className="mt-2 flex justify-between border-t border-[#E2E8E4] pt-2 font-semibold text-[#1B4332]">
                          <span>
                            Final Total (incl. ${order.platformFeeAmount.toFixed(2)} fee + $
                            {order.deliveryFeeAmount.toFixed(2)} deliv.)
                          </span>
                          <span>${order.finalTotalAmount.toFixed(2)}</span>
                        </div>

                        {canBuyerConfirm && (
                          <div className="mt-3 rounded-lg border border-amber-200 bg-amber-50 p-2.5">
                            <p className="font-semibold text-amber-900">
                              {order.status === "Disputed"
                                ? "Order disputed — approve updated weight once resolved."
                                : "Weight variance (> ±10%) or rejected produce requires buyer sign-off."}
                            </p>
                            <div className="mt-2 flex flex-wrap gap-2">
                              <button
                                type="button"
                                disabled={confirmingId === order.id}
                                onClick={() => void handleConfirmReceipt(order.id, true)}
                                className="rounded-lg bg-[#1B4332] px-3 py-1.5 text-xs font-semibold text-white hover:bg-[#2D6A4F] disabled:opacity-50"
                              >
                                {confirmingId === order.id ? "Saving..." : "Approve Final Weight"}
                              </button>
                              {order.status === "DeliveredPendingApproval" && (
                                <button
                                  type="button"
                                  disabled={confirmingId === order.id}
                                  onClick={() => void handleConfirmReceipt(order.id, false)}
                                  className="rounded-lg border border-red-200 bg-white px-3 py-1.5 text-xs font-semibold text-red-700 hover:bg-red-50 disabled:opacity-50"
                                >
                                  Dispute Delivery
                                </button>
                              )}
                            </div>
                          </div>
                        )}

                        {isAdmin && order.status !== "Cancelled" && (
                          <div className="mt-2 flex flex-wrap items-center justify-end gap-1.5 border-t border-[#E2E8E4] pt-2">
                            {order.paymentStatus !== "Paid" ? (
                              <>
                                <button
                                  type="button"
                                  disabled={payingOrderId === order.id}
                                  onClick={() =>
                                    void handleSetPaymentStatus(order.id, "Paid", "CashOnDelivery")
                                  }
                                  className="rounded-lg bg-emerald-700 px-2.5 py-1 text-[11px] font-semibold text-white hover:bg-emerald-800 disabled:opacity-50"
                                >
                                  Mark Paid (COD)
                                </button>
                                <button
                                  type="button"
                                  disabled={payingOrderId === order.id}
                                  onClick={() =>
                                    void handleSetPaymentStatus(order.id, "Paid", "BankTransfer")
                                  }
                                  className="rounded-lg border border-emerald-700 px-2.5 py-1 text-[11px] font-semibold text-emerald-800 hover:bg-emerald-50 disabled:opacity-50"
                                >
                                  Bank Transfer Paid
                                </button>
                                <button
                                  type="button"
                                  disabled={payingOrderId === order.id}
                                  onClick={() =>
                                    void handleSetPaymentStatus(order.id, "Paid", "WeeklyInvoice")
                                  }
                                  className="rounded-lg border border-stone-300 px-2.5 py-1 text-[11px] font-semibold text-stone-700 hover:bg-stone-100 disabled:opacity-50"
                                >
                                  Weekly Invoice Paid
                                </button>
                              </>
                            ) : (
                              <button
                                type="button"
                                disabled={payingOrderId === order.id}
                                onClick={() => void handleSetPaymentStatus(order.id, "Unpaid")}
                                className="rounded-lg border border-stone-300 px-2.5 py-1 text-[11px] font-semibold text-stone-600 hover:bg-stone-100 disabled:opacity-50"
                              >
                                Revert to Unpaid
                              </button>
                            )}
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              </section>
            );
          })}
        </div>
      )}
    </div>
  );
}
