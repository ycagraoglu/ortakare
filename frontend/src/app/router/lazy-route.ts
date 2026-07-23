import { lazy, type ComponentType, type LazyExoticComponent } from "react";

type ModuleLoader<TComponent extends ComponentType> = () => Promise<{
  default: TComponent;
}>;

export type PreloadableComponent<TComponent extends ComponentType = ComponentType> =
  LazyExoticComponent<TComponent> & {
    preload: ModuleLoader<TComponent>;
  };

export function lazyRoute<TComponent extends ComponentType>(
  loader: ModuleLoader<TComponent>,
): PreloadableComponent<TComponent> {
  const component = lazy(loader) as PreloadableComponent<TComponent>;
  component.preload = loader;
  return component;
}
