"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { ApiError, ordersApi } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import type { CheckoutGroup, Order } from "@/lib/types";

export default function OrdersPage() {
  const { user } = useAuth();
  const [groups, setGroups] = useState<CheckoutGroup[]>([]);
  const [farmerOrders, setFarmerOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [repeatingId, setRepeatingId] = useState<string | null>(null);

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
                  {group.orders.map((order) => (
                    <div
                      key={order.id}
                      className="rounded-xl border border-[#E2E8E4] bg-[#FAF9F6] p-3.5 text-xs"
                    >
                      <div className="flex items-center justify-between">
                        <span className="font-bold text-[#1F2933]">
                          {order.farmerName ?? "Farm"}
                        </span>
                        <span className="rounded-full bg-[#D8F3DC] px-2.5 py-0.5 font-semibold text-[#1B4332]">
                          {order.status}
                        </span>
                      </div>
                      <div className="mt-2 space-y-1">
                        {order.items.map((item) => (
                          <div key={item.id} className="flex justify-between text-stone-700">
                            <span>
                              {item.productTitleSnapshot} ({item.estimatedWeightKg} kg @ $
                              {item.unitPriceApplied.toFixed(2)})
                            </span>
                            <span>
                              ${(item.estimatedWeightKg * item.unitPriceApplied).toFixed(2)}
                            </span>
                          </div>
                        ))}
                      </div>
                      <div className="mt-2 flex justify-between border-t border-[#E2E8E4] pt-2 font-semibold text-[#1B4332]">
                        <span>
                          Total (incl. ${order.platformFeeAmount.toFixed(2)} fee + $
                          {order.deliveryFeeAmount.toFixed(2)} deliv.)
                        </span>
                        <span>${order.finalTotalAmount.toFixed(2)}</span>
                      </div>
                    </div>
                  ))}
                </div>
              </section>
            );
          })}
        </div>
      )}
    </div>
  );
}
